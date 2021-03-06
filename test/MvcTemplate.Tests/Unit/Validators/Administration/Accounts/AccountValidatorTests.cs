﻿using MvcTemplate.Components.Alerts;
using MvcTemplate.Components.Security;
using MvcTemplate.Data.Core;
using MvcTemplate.Objects;
using MvcTemplate.Resources.Views.AccountView;
using MvcTemplate.Tests.Data;
using MvcTemplate.Validators;
using NSubstitute;
using System;
using System.Linq;
using System.Web.Mvc;
using Xunit;

namespace MvcTemplate.Tests.Unit.Validators
{
    public class AccountValidatorTests : IDisposable
    {
        private AccountValidator validator;
        private TestingContext context;
        private IHasher hasher;

        public AccountValidatorTests()
        {
            context = new TestingContext();
            hasher = Substitute.For<IHasher>();
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(true);

            validator = new AccountValidator(new UnitOfWork(context), hasher);
            validator.ModelState = new ModelStateDictionary();

            TearDownData();
            SetUpData();
        }
        public void Dispose()
        {
            validator.Dispose();
            context.Dispose();
        }

        #region Method: CanRegister(AccountRegisterView view)

        [Fact]
        public void CanRegister_CanNotRegisterWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Key", "Error");

            Assert.False(validator.CanRegister(ObjectFactory.CreateAccountRegisterView()));
        }

        [Fact]
        public void CanRegister_CanNotRegisterWithAlreadyTakenUsername()
        {
            AccountRegisterView view = ObjectFactory.CreateAccountRegisterView();
            view.Username = view.Username.ToLower();
            view.Id += "DifferentValue";

            Assert.False(validator.CanRegister(view));
        }

        [Fact]
        public void CanRegister_AddsErorrMessageThenCanNotRegisterWithAlreadyTakenUsername()
        {
            AccountRegisterView view = ObjectFactory.CreateAccountRegisterView();
            view.Username = view.Username.ToLower();
            view.Id += "DifferentValue";

            validator.CanRegister(view);

            String actual = validator.ModelState["Username"].Errors[0].ErrorMessage;
            String expected = Validations.UsernameIsAlreadyTaken;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanRegister_CanNotRegisterWithAlreadyUsedEmail()
        {
            AccountRegisterView view = ObjectFactory.CreateAccountRegisterView();
            view.Id += "DifferentValue";

            Assert.False(validator.CanRegister(view));
        }

        [Fact]
        public void CanRegister_AddsErrorMessageThenCanNotRegisterWithAlreadyUsedEmail()
        {
            AccountRegisterView view = ObjectFactory.CreateAccountRegisterView();
            view.Id += "DifferentValue";

            validator.CanRegister(view);

            String actual = validator.ModelState["Email"].Errors[0].ErrorMessage;
            String expected = Validations.EmailIsAlreadyUsed;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanRegister_CanRegisterValidAccount()
        {
            Assert.True(validator.CanRegister(ObjectFactory.CreateAccountRegisterView(2)));
        }

        #endregion

        #region Method: CanRecover(AccountRecoveryView view)

        [Fact]
        public void CanRecover_CanNotRecoverAccountWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Test", "Test");

            Assert.False(validator.CanRecover(ObjectFactory.CreateAccountRecoveryView()));
        }

        [Fact]
        public void CanRecover_CanRecoverValidAccount()
        {
            Assert.True(validator.CanRecover(ObjectFactory.CreateAccountRecoveryView()));
        }

        #endregion

        #region Method: CanReset(AccountResetView view)

        [Fact]
        public void CanReset_CanNotResetAccountWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Test", "Test");

            Assert.False(validator.CanReset(ObjectFactory.CreateAccountResetView()));
        }

        [Fact]
        public void CanReset_CanNotResetAccountWithExpiredToken()
        {
            Account account = context.Set<Account>().Single();
            account.RecoveryTokenExpirationDate = DateTime.Now.AddMinutes(-5);
            context.SaveChanges();

            Assert.False(validator.CanReset(ObjectFactory.CreateAccountResetView()));
        }

        [Fact]
        public void CanReset_AddsErorrMessageThenCanNotResetAccountWithExpiredToken()
        {
            Account account = context.Set<Account>().Single();
            account.RecoveryTokenExpirationDate = DateTime.Now.AddMinutes(-5);
            context.SaveChanges();

            validator.CanReset(ObjectFactory.CreateAccountResetView());

            Alert actual = validator.Alerts.Single();

            Assert.Equal(Validations.RecoveryTokenExpired, actual.Message);
            Assert.Equal(AlertType.Danger, actual.Type);
            Assert.Equal(0, actual.FadeoutAfter);
        }

        [Fact]
        public void CanReset_CanResetValidAccount()
        {
            Assert.True(validator.CanRecover(ObjectFactory.CreateAccountRecoveryView()));
        }

        #endregion

        #region Method: CanLogin(AccountLoginView view)

        [Fact]
        public void CanLogin_CanNotLoginWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Key", "ErrorMesages");

            Assert.False(validator.CanLogin(ObjectFactory.CreateAccountLoginView()));
        }

        [Fact]
        public void CanLogin_CanNotLoginFromNonExistingAccount()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            AccountLoginView view = new AccountLoginView();

            Assert.False(validator.CanLogin(view));
        }

        [Fact]
        public void CanLogin_AddsErrorMessageThenCanNotLoginWithNotExistingAccount()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            AccountLoginView view = new AccountLoginView();

            validator.CanLogin(view);

            String actual = validator.ModelState[""].Errors[0].ErrorMessage;
            String expected = Validations.IncorrectUsernameOrPassword;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanLogin_CanNotLoginWithIncorrectPassword()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            AccountLoginView view = ObjectFactory.CreateAccountLoginView();

            Assert.False(validator.CanLogin(view));
        }

        [Fact]
        public void CanLogin_AddsErrorMessageThenCanNotLoginWithIncorrectPassword()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            AccountLoginView view = ObjectFactory.CreateAccountLoginView();
            view.Password += "Incorrect";

            validator.CanLogin(view);

            String actual = validator.ModelState[""].Errors[0].ErrorMessage;
            String expected = Validations.IncorrectUsernameOrPassword;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanLogin_CanLoginWithCaseInsensitiveUsername()
        {
            AccountLoginView view = ObjectFactory.CreateAccountLoginView();
            view.Username = view.Username.ToUpper();

            Assert.True(validator.CanLogin(view));
        }

        [Fact]
        public void CanLogin_CanLoginWithValidAccount()
        {
            Assert.True(validator.CanLogin(ObjectFactory.CreateAccountLoginView()));
        }

        #endregion

        #region Method: CanCreate(AccountCreateView view)

        [Fact]
        public void CanCreate_CanNotCreateWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Key", "Error");

            Assert.False(validator.CanCreate(ObjectFactory.CreateAccountCreateView()));
        }

        [Fact]
        public void CanCreate_CanNotCreateWithAlreadyTakenUsername()
        {
            AccountCreateView view = ObjectFactory.CreateAccountCreateView();
            view.Username = view.Username.ToLower();
            view.Id += "DifferentValue";

            Assert.False(validator.CanCreate(view));
        }

        [Fact]
        public void CanCreate_AddsErorrMessageThenCanNotCreateWithAlreadyTakenUsername()
        {
            AccountCreateView view = ObjectFactory.CreateAccountCreateView();
            view.Username = view.Username.ToLower();
            view.Id += "DifferentValue";

            validator.CanCreate(view);

            String actual = validator.ModelState["Username"].Errors[0].ErrorMessage;
            String expected = Validations.UsernameIsAlreadyTaken;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanCreate_CanNotCreateWithAlreadyUsedEmail()
        {
            AccountCreateView view = ObjectFactory.CreateAccountCreateView();
            view.Id += "DifferentValue";

            Assert.False(validator.CanCreate(view));
        }

        [Fact]
        public void CanCreate_AddsErrorMessageThenCanNotCreateWithAlreadyUsedEmail()
        {
            AccountCreateView view = ObjectFactory.CreateAccountCreateView();
            view.Id += "DifferentValue";

            validator.CanCreate(view);

            String actual = validator.ModelState["Email"].Errors[0].ErrorMessage;
            String expected = Validations.EmailIsAlreadyUsed;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanCreate_CanCreateValidAccount()
        {
            Assert.True(validator.CanCreate(ObjectFactory.CreateAccountCreateView(2)));
        }

        #endregion

        #region Method: CanEdit(ProfileEditView view)

        [Fact]
        public void CanEdit_CanNotEditWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Key", "ErrorMessages");

            Assert.False(validator.CanEdit(ObjectFactory.CreateProfileEditView()));
        }

        [Fact]
        public void CanEdit_CanNotEditWithIncorrectPassword()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Password += "1";

            Assert.False(validator.CanEdit(view));
        }

        [Fact]
        public void CanEdit_AddsErrorMessageThenCanNotEditWithIncorrectPassword()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Password += "1";

            validator.CanEdit(view);

            String expected = Validations.IncorrectPassword;
            String actual = validator.ModelState["Password"].Errors[0].ErrorMessage;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanEdit_CanNotEditToAlreadyTakenUsername()
        {
            Account takenAccount = ObjectFactory.CreateAccount(2);
            context.Set<Account>().Add(takenAccount);
            context.SaveChanges();

            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Username = takenAccount.Username;

            Assert.False(validator.CanEdit(view));
        }

        [Fact]
        public void CanEdit_AddsErrorMessageThenCanNotEditToAlreadyTakenUsername()
        {
            Account takenAccount = ObjectFactory.CreateAccount(2);
            context.Set<Account>().Add(takenAccount);
            context.SaveChanges();

            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Username = takenAccount.Username;

            validator.CanEdit(view);

            String actual = validator.ModelState["Username"].Errors[0].ErrorMessage;
            String expected = Validations.UsernameIsAlreadyTaken;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanEdit_CanEditUsingItsOwnUsername()
        {
            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Username = view.Username.ToUpper();

            Assert.True(validator.CanEdit(view));
        }

        [Fact]
        public void CanEdit_CanNotEditToAlreadyUsedEmail()
        {
            Account usedEmailAccount = ObjectFactory.CreateAccount(2);
            context.Set<Account>().Add(usedEmailAccount);
            context.SaveChanges();

            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Email = usedEmailAccount.Email;

            Assert.False(validator.CanEdit(view));
        }

        [Fact]
        public void CanEdit_AddsErorrMessageThenCanNotEditToAlreadyUsedEmail()
        {
            Account usedEmailAccount = ObjectFactory.CreateAccount(2);
            context.Set<Account>().Add(usedEmailAccount);
            context.SaveChanges();

            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Email = usedEmailAccount.Email;

            validator.CanEdit(view);

            String actual = validator.ModelState["Email"].Errors[0].ErrorMessage;
            String expected = Validations.EmailIsAlreadyUsed;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanEdit_CanEditUsingItsOwnEmail()
        {
            ProfileEditView view = ObjectFactory.CreateProfileEditView();
            view.Email = view.Email.ToUpper();

            Assert.True(validator.CanEdit(view));
        }

        [Fact]
        public void CanEdit_CanEditValidProfile()
        {
            Assert.True(validator.CanEdit(ObjectFactory.CreateProfileEditView()));
        }

        #endregion

        #region Method: CanEdit(AccountEditView view)

        [Fact]
        public void CanEdit_CanNotEditAccountWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Test", "Test");

            Assert.False(validator.CanEdit(ObjectFactory.CreateAccountEditView()));
        }

        [Fact]
        public void CanEdit_CanEditValidAccount()
        {
            Assert.True(validator.CanEdit(ObjectFactory.CreateAccountEditView()));
        }

        #endregion

        #region Method: CanDelete(ProfileDeleteView view)

        [Fact]
        public void CanEdit_CanNotDeleteWithInvalidModelState()
        {
            validator.ModelState.AddModelError("Test", "Test");

            Assert.False(validator.CanDelete(ObjectFactory.CreateProfileDeleteView()));
        }

        [Fact]
        public void CanDelete_CanNotDeleteWithIncorrectPassword()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            ProfileDeleteView view = ObjectFactory.CreateProfileDeleteView();
            view.Password += "1";

            Assert.False(validator.CanDelete(view));
        }

        [Fact]
        public void CanDelete_AddsErrorMessageThenCanNotDeleteWithIncorrectPassword()
        {
            hasher.VerifyPassword(Arg.Any<String>(), Arg.Any<String>()).Returns(false);
            ProfileDeleteView view = ObjectFactory.CreateProfileDeleteView();
            view.Password += "1";

            validator.CanDelete(view);

            String expected = Validations.IncorrectPassword;
            String actual = validator.ModelState["Password"].Errors[0].ErrorMessage;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanDelete_CanDeleteValidProfileDeleteView()
        {
            Assert.True(validator.CanDelete(ObjectFactory.CreateProfileDeleteView()));
        }

        #endregion

        #region Test helpers

        private void SetUpData()
        {
            Account account = ObjectFactory.CreateAccount();
            account.Role = ObjectFactory.CreateRole();
            account.RoleId = account.Role.Id;

            context.Set<Account>().Add(account);
            context.SaveChanges();
        }
        private void TearDownData()
        {
            context.Set<RolePrivilege>().RemoveRange(context.Set<RolePrivilege>());
            context.Set<Account>().RemoveRange(context.Set<Account>());
            context.Set<Role>().RemoveRange(context.Set<Role>());
            context.SaveChanges();
        }

        #endregion
    }
}
