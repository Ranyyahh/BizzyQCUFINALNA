using System;
using System.Linq;
using System.Web.Mvc;
using BizzyQCU.Models.Landingpage;

namespace BizzyQCU.Controllers
{
    public class EnterpriseDashboardController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult EnterpriseDashboard()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string role = Session["Role"]?.ToString();
            if (role != "enterprise")
            {
                return RedirectToAction("ProductList", "ProductList");
            }

            int userId = (int)Session["UserId"];
            var enterprise = db.GetEnterpriseByUserId(userId);

            if (enterprise == null)
            {
                return RedirectToAction("Login", "Home");
            }

            ViewBag.EnterpriseName = enterprise.StoreName;
            ViewBag.StoreLogo = enterprise.StoreLogo;
            return View();
        }

        public ActionResult EnterpriseChatroom(int? orderId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            string role = Session["Role"]?.ToString();
            if (role != "enterprise")
            {
                return RedirectToAction("ProductList", "ProductList");
            }

            int userId = (int)Session["UserId"];
            var enterprise = db.GetEnterpriseByUserId(userId);

            if (enterprise == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var orders = db.GetPendingOrders(enterprise.EnterpriseId) ?? new System.Collections.Generic.List<PendingOrder>();
            int activeOrderId = orderId ?? orders.FirstOrDefault()?.OrderId ?? 0;

            ViewBag.EnterpriseName = enterprise.StoreName;
            ViewBag.ActiveOrderId = activeOrderId;

            return View(orders);
        }

        // ============================================
        // API: GET ENTERPRISE STATS
        // ============================================
        [HttpGet]
        public JsonResult GetStats()
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" }, JsonRequestBehavior.AllowGet);
                }

                int enterpriseId = enterprise.EnterpriseId;

                // Get today's sales
                decimal totalSalesToday = db.GetTotalSalesToday(enterpriseId);

                // Get pending orders count
                int ordersPending = db.GetPendingOrdersCount(enterpriseId);

                // Get active deliveries count
                int deliveriesActive = db.GetActiveDeliveriesCount(enterpriseId);

                // Get new orders count (today)
                int newOrdersCount = db.GetNewOrdersCountToday(enterpriseId);

                return Json(new
                {
                    success = true,
                    totalSalesToday = totalSalesToday,
                    ordersPending = ordersPending,
                    deliveriesActive = deliveriesActive,
                    newOrdersCount = newOrdersCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetStats Error: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================
        // API: GET CHART DATA (DAILY/MONTHLY)
        // ============================================
        [HttpGet]
        public JsonResult GetChartData(string period)
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Not logged in" }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" }, JsonRequestBehavior.AllowGet);
                }

                int enterpriseId = enterprise.EnterpriseId;

                if (period == "daily" || period == "weekly")
                {
                    var weeklySales = db.GetWeeklySalesData(userId);
                    return Json(new
                    {
                        success = true,
                        labels = weeklySales.Select(x => x.DayName).ToArray(),
                        values = weeklySales.Select(x => x.Sales).ToArray(),
                        total = period == "weekly"
                            ? $"Weekly Sales: ₱ {weeklySales.Sum(x => x.Sales):N0}"
                            : $"This Week: ₱ {weeklySales.Sum(x => x.Sales):N0}"
                    }, JsonRequestBehavior.AllowGet);
                }
                else // monthly
                {
                    var monthlySales = db.GetMonthlySalesData(userId);
                    return Json(new
                    {
                        success = true,
                        labels = monthlySales.Select(x => x.MonthName).ToArray(),
                        values = monthlySales.Select(x => x.Sales).ToArray(),
                        total = $"This Month: ₱ {monthlySales.Sum(x => x.Sales):N0}"
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetChartData Error: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
