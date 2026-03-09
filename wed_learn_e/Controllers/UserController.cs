using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace wed_learn_e.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult dangky()
        {

            return View();
        }
        public ActionResult dangnhap()
        {
            return View();
        }
    }
}