using BizzyQCU.Models.Landingpage;
using System;
using System.IO;
using System.Web.Mvc;

namespace BizzyQCU.Controllers
{
    public class AddProductController : Controller
    {
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult AddProduct()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            string role = Session["Role"]?.ToString();
            if (role != "enterprise")
            {
                return RedirectToAction("ProductList", "ProductList");
            }

            return View();
        }

        [HttpPost]
        public JsonResult AddProductAjax()
        {
            try
            {
                if (Session["UserId"] == null)
                {
                    return Json(new { success = false, message = "Please login first" });
                }

                int userId = (int)Session["UserId"];
                var enterprise = db.GetEnterpriseByUserId(userId);

                if (enterprise == null)
                {
                    return Json(new { success = false, message = "Enterprise not found" });
                }

                string name = Request.Form["name"];
                decimal price;
                string category = Request.Form["category"];
                string preparationTime = Request.Form["preparationTime"];
                string description = Request.Form["description"];

                if (!decimal.TryParse(Request.Form["price"], out price))
                {
                    return Json(new { success = false, message = "Invalid price format" });
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Product name is required" });
                }

                if (price <= 0)
                {
                    return Json(new { success = false, message = "Price must be greater than 0" });
                }

                byte[] productImage = null;
                if (Request.Files.Count > 0 && Request.Files[0] != null && Request.Files[0].ContentLength > 0)
                {
                    var file = Request.Files[0];
                    using (var binaryReader = new BinaryReader(file.InputStream))
                    {
                        productImage = binaryReader.ReadBytes(file.ContentLength);
                    }
                }

                int? categoryId = null;
                if (!string.IsNullOrWhiteSpace(category))
                {
                    categoryId = db.GetOrCreateCategory(category);
                }

                int prepTime;
                if (string.IsNullOrWhiteSpace(preparationTime) || !int.TryParse(preparationTime, out prepTime))
                {
                    return Json(new { success = false, message = "Preparation time must be in whole minutes only." });
                }

                if (prepTime < 1 || prepTime > 60)
                {
                    return Json(new { success = false, message = "Preparation time must be between 1 and 60 minutes." });
                }

                // Add product with pending approval
                string dbError;
                bool result = db.AddProductWithApproval(
                    enterprise.EnterpriseId,
                    name,
                    description ?? "",
                    price,
                    categoryId,
                    prepTime,
                    productImage,
                    out dbError
                );

                if (result)
                {
                    return Json(new { success = true, message = "Product submitted for approval! Admin will review it shortly." });
                }
                else
                {
                    var safeMessage = string.IsNullOrWhiteSpace(dbError)
                        ? "Failed to submit product"
                        : "Failed to submit product: " + dbError;
                    return Json(new { success = false, message = safeMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
