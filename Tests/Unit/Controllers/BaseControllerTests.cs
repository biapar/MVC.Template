﻿using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Web.Mvc;
using System.Web.Routing;
using Template.Components.Security;
using Template.Tests.Helpers;

namespace Template.Tests.Unit.Controllers
{
    [TestFixture]
    public class BaseControllerTests
    {
        private Mock<BaseControllerStub> controllerMock;
        private Mock<IRoleProvider> roleProviderMock;
        private BaseControllerStub baseController;
        private String accountId;

        [SetUp]
        public void SetUp()
        {
            HttpMock httpMock = new HttpMock();
            controllerMock = new Mock<BaseControllerStub>() { CallBase = true };
            RequestContext requestContext = httpMock.HttpContext.Request.RequestContext;

            accountId = httpMock.HttpContextBase.User.Identity.Name;
            controllerMock.Object.Url = new UrlHelper(requestContext);
            controllerMock.Object.ControllerContext = new ControllerContext();
            controllerMock.Object.ControllerContext.HttpContext = httpMock.HttpContextBase;
            controllerMock.Object.ControllerContext.RouteData = httpMock.HttpContextBase.Request.RequestContext.RouteData;

            roleProviderMock = new Mock<IRoleProvider>(MockBehavior.Strict);
            baseController = controllerMock.Object;
        }

        [TearDown]
        public void TearDown()
        {
            RoleProviderFactory.SetInstance(null);
        }

        #region Constructor: BaseController()

        [Test]
        public void BaseController_SetsRoleProviderFromFactory()
        {
            RoleProviderFactory.SetInstance(roleProviderMock.Object);
            baseController = new BaseControllerStub();

            IRoleProvider expected = RoleProviderFactory.Instance;
            IRoleProvider actual = baseController.BaseRoleProvider;

            Assert.AreEqual(expected, actual);
        }

        #endregion

        #region Method: RedirectIfAuthorized(String action)

        [Test]
        public void RedirectIfAuthorized_RedirectsToDefaultIfNotAuthorized()
        {
            RedirectToRouteResult expected = new RedirectToRouteResult(new RouteValueDictionary());
            controllerMock.Protected().Setup<RedirectToRouteResult>("RedirectToDefault").Returns(expected);
            controllerMock.Protected().Setup<Boolean>("IsAuthorizedFor", "Action").Returns(false);

            RedirectToRouteResult actual = baseController.BaseRedirectIfAuthorized("Action");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void RedirectIfAuthorized_RedirectsToActionIfAuthorized()
        {
            controllerMock.Protected().Setup<Boolean>("IsAuthorizedFor", "Action").Returns(true);

            RouteValueDictionary actual = baseController.BaseRedirectIfAuthorized("Action").RouteValues;
            RouteValueDictionary expected = baseController.BaseRedirectToAction("Action").RouteValues;

            Assert.AreEqual(expected["language"], actual["language"]);
            Assert.AreEqual(expected["controller"], actual["controller"]);
            Assert.AreEqual(expected["action"], actual["action"]);
            Assert.AreEqual(expected["area"], actual["area"]);
        }

        #endregion

        #region Method: RedirectToLocal(String url)

        [Test]
        public void RedirectToLocal_RedirectsToDefaultIfUrlIsNotLocal()
        {
            RedirectToRouteResult expected = new RedirectToRouteResult(new RouteValueDictionary());
            controllerMock.Protected().Setup<RedirectToRouteResult>("RedirectToDefault").Returns(expected);
            RedirectToRouteResult actual = baseController.BaseRedirectToLocal("http://www.test.com") as RedirectToRouteResult;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void RedirectToLocal_RedirectsToLocalIfUrlIsLocal()
        {
            String expected = "/Home/Index";
            String actual = (baseController.BaseRedirectToLocal(expected) as RedirectResult).Url;

            Assert.AreEqual(expected, actual);
        }

        #endregion

        #region Method: RedirectToDefault()

        [Test]
        public void RedirectToDefault_RedirectsToDefault()
        {
            baseController.RouteData.Values["language"] = "lt-LT";
            RouteValueDictionary actual = baseController.BaseRedirectToDefault().RouteValues;

            Assert.AreEqual(String.Empty, actual["controller"]);
            Assert.AreEqual(String.Empty, actual["action"]);
            Assert.AreEqual(String.Empty, actual["area"]);
            Assert.AreEqual("lt-LT", actual["language"]);
        }

        #endregion

        #region Method: RedirectToUnauthorized()

        [Test]
        public void RedirectsToUnauthorized_RedirectsToHomeUnauthorized()
        {
            baseController.RouteData.Values["language"] = "lt-LT";
            RouteValueDictionary actual = baseController.BaseRedirectToUnauthorized().RouteValues;

            Assert.AreEqual("lt-LT", actual["language"]);
            Assert.AreEqual(String.Empty, actual["area"]);
            Assert.AreEqual("Home", actual["controller"]);
            Assert.AreEqual("Unauthorized", actual["action"]);
        }

        #endregion

        #region Method: OnAuthorization(AuthorizationContext filterContext)

        [Test]
        public void OnAuthorization_SetsResultToRedirectToUnauthorizedIfNotAuthorized()
        {
            Mock<ActionDescriptor> actionDescriptorMock = new Mock<ActionDescriptor>() { CallBase = true };
            AuthorizationContext filterContext = new AuthorizationContext(baseController.ControllerContext, actionDescriptorMock.Object);

            String controller = baseController.RouteData.Values["controller"] as String;
            String action = baseController.RouteData.Values["action"] as String;
            String area = baseController.RouteData.Values["area"] as String;

            RedirectToRouteResult expected = new RedirectToRouteResult(new RouteValueDictionary());
            controllerMock.Protected().Setup<RedirectToRouteResult>("RedirectToUnauthorized").Returns(expected);
            roleProviderMock.Setup(mock => mock.IsAuthorizedFor(accountId, area, controller, action)).Returns(false);
            baseController.BaseRoleProvider = roleProviderMock.Object;
            baseController.BaseOnAuthorization(filterContext);

            RedirectToRouteResult actual = filterContext.Result as RedirectToRouteResult;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OnAuthorization_SetsResultToNullThenAuthorized()
        {
            Mock<ActionDescriptor> actionDescriptorMock = new Mock<ActionDescriptor>() { CallBase = true };
            AuthorizationContext filterContext = new AuthorizationContext(baseController.ControllerContext, actionDescriptorMock.Object);
            filterContext.RouteData.Values["controller"] = "Controller";
            filterContext.RouteData.Values["action"] = "Action";
            filterContext.RouteData.Values["area"] = "Area";

            roleProviderMock.Setup(mock => mock.IsAuthorizedFor(accountId, "Area", "Controller", "Action")).Returns(true);
            baseController.BaseRoleProvider = roleProviderMock.Object;
            baseController.BaseOnAuthorization(filterContext);

            Assert.IsNull(filterContext.Result);
        }

        #endregion

        #region Method: IsAuthorizedFor(String action)

        [Test]
        public void IsAuthorizedFor_ReturnsTrueThenAuthorized()
        {
            controllerMock.Protected().Setup<Boolean>("IsAuthorizedFor", "Area", "Controller", "Action").Returns(true);
            baseController.RouteData.Values["controller"] = "Controller";
            baseController.RouteData.Values["area"] = "Area";

            Assert.IsTrue(baseController.BaseIsAuthorizedFor("Action"));
        }

        [Test]
        public void IsAuthorizedFor_ReturnsFalseThenNotAuthorized()
        {
            controllerMock.Protected().Setup<Boolean>("IsAuthorizedFor", "Area", "Controller", "Action").Returns(false);
            baseController.RouteData.Values["controller"] = "Controller";
            baseController.RouteData.Values["area"] = "Area";

            Assert.IsFalse(baseController.BaseIsAuthorizedFor("Action"));
        }

        #endregion

        #region Method: IsAuthorizedFor(String area, String controller, String action)

        [Test]
        public void IsAuthorizedFor_OnNullRoleProviderReturnsTrue()
        {
            baseController.BaseRoleProvider = null;

            Assert.IsTrue(baseController.BaseIsAuthorizedFor(null, null, null));
        }

        [Test]
        public void IsAuthorizedFor_ReturnsRoleProviderResult()
        {
            roleProviderMock.Setup(mock => mock.IsAuthorizedFor(accountId, "AR", "CO", "AC")).Returns(true);
            baseController.BaseRoleProvider = roleProviderMock.Object;

            Assert.IsTrue(baseController.BaseIsAuthorizedFor("AR", "CO", "AC"));
        }

        #endregion
    }
}
