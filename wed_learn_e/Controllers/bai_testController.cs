using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace wed_learn_e.Controllers
{
    public class bai_testController : Controller
    {
        // GET: bai_test
        public ActionResult nav_baitest()
        {
            return View();
        }
        public ActionResult bai_test()
        {
                       return View();

        }
        public ActionResult thong_tin_bai_test()
        {
            return View();
        }
        public ActionResult end_bai_test()
        {
            return View();
        }
    }
}