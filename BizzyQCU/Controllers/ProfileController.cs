using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BizzyQCU.Models.Landingpage;

namespace BizzyQCU.Controllers
{
    // ==================== MODELS ====================
    [Serializable]
    public class EnterpriseSummary
    {
        public string PhotoDataUrl { get; set; }
        public string QrDataUrl { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Gcash { get; set; }
        public string Role { get; set; }
    }

    [Serializable]
    public class ManagerSummary
    {
        public string Name { get; set; }
        public string Section { get; set; }
        public string StudentId { get; set; }
        public string ContactNumber { get; set; }
    }

    [Serializable]
    public class EnterpriseStatsSummary
    {
        public int OrdersCompleted { get; set; }
        public int ProductsListed { get; set; }
        public decimal TotalSales { get; set; }
    }

    [Serializable]
    public class EnterpriseDashboardViewModel
    {
        public EnterpriseSummary Enterprise { get; set; }
        public ManagerSummary Manager { get; set; }
        public EnterpriseStatsSummary Stats { get; set; }
    }

    [Serializable]
    public class UserProfileViewModel
    {
        public string PhotoDataUrl { get; set; }
        public string QrDataUrl { get; set; }

        [Required(ErrorMessage = "Enterprise name is required.")]
        [Display(Name = "Enterprise Name")]
        public string EnterpriseName { get; set; }

        [Required(ErrorMessage = "Enterprise type is required.")]
        [Display(Name = "Enterprise Type")]
        public string EnterpriseType { get; set; }

        [Required(ErrorMessage = "Gcash number is required.")]
        [Display(Name = "Gcash No.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "Enter an 11-digit Gcash number starting with 09.")]
        public string Contact { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; }

        [Display(Name = "Manager Name")]
        public string ManagerName { get; set; }

        [Display(Name = "Student ID")]
        [RegularExpression(@"^$|^\d{2}-\d{4}$", ErrorMessage = "Use the format 00-0000.")]
        public string StudentId { get; set; }

        [Display(Name = "Section")]
        public string Section { get; set; }

        [Display(Name = "Manager Gcash No.")]
        [RegularExpression(@"^$|^09\d{9}$", ErrorMessage = "Enter an 11-digit Gcash number starting with 09.")]
        public string ManagerContactNumber { get; set; }

        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Birthdate { get; set; }
        public string Address { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm the new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "New password and confirmation do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class AccountSettingsPageViewModel
    {
        public UserProfileViewModel Profile { get; set; }
        public ChangePasswordViewModel PasswordChange { get; set; }
        public bool ReopenPasswordModal { get; set; }
    }

    [Serializable]
    public class TransactionRecordViewModel
    {
        public string UserName { get; set; }
        public string Product { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public string StatusLabel { get; set; }
    }

    public class TransactionsPageViewModel
    {
        public string SearchTerm { get; set; }
        public IList<TransactionRecordViewModel> Transactions { get; set; }
    }

    public class UserProfileApiRequest
    {
        public string ManagerName { get; set; }
        public string ManagerContactNumber { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string StudentNumber { get; set; }
        public string Section { get; set; }
        public string Birthdate { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string PhotoDataUrl { get; set; }
    }

    public class ProfileController : Controller
    {
        private const string ProfileSessionKey = "ProfileState";
        private const string PasswordSessionKey = "ProfilePassword";
        private readonly SimpleDb db = new SimpleDb();

        public ActionResult Index()
        {
            ViewBag.PageClass = "page-dashboard";
            ViewBag.ShowBackButton = true;
            ViewBag.NavSection = "home";
            return View(BuildDashboardViewModel());
        }

        [ActionName("Homepage")]
        public ActionResult Homepage()
        {
            return RedirectToAction("Index");
        }

        public ActionResult Products()
        {
            ViewBag.PageClass = "page-products";
            ViewBag.ShowBackButton = true;
            ViewBag.NavSection = "none";
            ViewBag.Title = "Products";
            return View("Products");
        }

        [ActionName("ProductList")]
        public ActionResult ProductListPage()
        {
            return RedirectToAction("Products");
        }

        [ActionName("Enterprise")]
        public ActionResult Enterprise()
        {
            return RedirectToAction("Enterprises");
        }

        public ActionResult Enterprises()
        {
            ViewBag.PageClass = "page-enterprises";
            ViewBag.ShowBackButton = true;
            ViewBag.NavSection = "enterprises";
            ViewBag.Title = "Enterprises";
            return View("Enterprises");
        }


        // ==================== UPDATED USERPROFILE ACTION ====================
        public ActionResult UserProfile()
        {
            PrepareUserProfilePage();

            // ADDED: These ViewBag variables are needed for the UserProfile.cshtml
            ViewBag.ShowProfileEditTools = false;
            ViewBag.ShowProfileSaveActions = false;

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                TempData["FlashMessage"] = "Please log in first.";
                return RedirectToAction("Login", "Home");
            }

            var profile = BuildUserProfileFromDatabase(userId) ?? new UserProfileViewModel
            {
                PhotoDataUrl = DefaultPhotoDataUrl(),
                QrDataUrl = string.Empty,
                EnterpriseName = string.Empty,
                EnterpriseType = string.Empty,
                Contact = string.Empty,
                Email = string.Empty,
                ManagerName = string.Empty,
                StudentId = string.Empty,
                Section = string.Empty,
                ManagerContactNumber = string.Empty
            };

            return View("UserProfile", BuildAccountSettingsViewModel(profile, new ChangePasswordViewModel()));
        }
        // ==================== END OF UPDATED USERPROFILE ACTION ====================

        public ActionResult AccountSettings()
        {
            PrepareProfilePage("AccountSettings", "none");
            return View("Profile", BuildAccountSettingsViewModel(GetProfile(), new ChangePasswordViewModel()));
        }

        [HttpPost]
        [ActionName("Profile")]
        [ValidateAntiForgeryToken]
        public ActionResult SaveProfile([Bind(Prefix = "Profile")] UserProfileViewModel model)
        {
            PrepareProfilePage("AccountSettings", "none");

            if (string.IsNullOrWhiteSpace(model.PhotoDataUrl))
            {
                model.PhotoDataUrl = DefaultPhotoDataUrl();
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", BuildAccountSettingsViewModel(model, new ChangePasswordViewModel()));
            }

            Session[ProfileSessionKey] = model;
            TempData["FlashMessage"] = "Profile information saved.";
            return RedirectToAction("AccountSettings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveUserProfile([Bind(Prefix = "Profile")] UserProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ShowProfileEditTools = false;
                ViewBag.ShowProfileSaveActions = false;
                return View("UserProfile", BuildAccountSettingsViewModel(model, new ChangePasswordViewModel()));
            }

            Session[ProfileSessionKey] = model;
            TempData["FlashMessage"] = "User profile saved successfully.";
            return RedirectToAction("UserProfile");
        }

        [HttpGet]
        public JsonResult GetUserProfile()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Json(new { success = false, message = "Please log in first." }, JsonRequestBehavior.AllowGet);
            }

            var profile = BuildUserProfileFromDatabase(userId);
            if (profile == null)
            {
                return Json(new { success = false, message = "No profile found for the logged-in account." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    managerName = profile.ManagerName ?? string.Empty,
                    managerContactNumber = profile.ManagerContactNumber ?? string.Empty,
                    firstname = profile.Firstname ?? string.Empty,
                    lastname = profile.Lastname ?? string.Empty,
                    studentNumber = profile.StudentId ?? string.Empty,
                    section = profile.Section ?? string.Empty,
                    birthdate = profile.Birthdate ?? string.Empty,
                    address = profile.Address ?? string.Empty,
                    email = profile.Email ?? string.Empty,
                    photoDataUrl = profile.PhotoDataUrl ?? DefaultPhotoDataUrl()
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult UpdateUserProfile(UserProfileApiRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid request payload." });
                }

                if (userId <= 0)
                {
                    return Json(new { success = false, message = "Please log in first." });
                }

                var isEnterpriseUser = db.GetEnterpriseByUserId(userId) != null;
                DateTime parsedBirthdate;
                DateTime? birthdate = DateTime.TryParse(request.Birthdate, out parsedBirthdate) ? (DateTime?)parsedBirthdate : null;

                var updated = isEnterpriseUser
                    ? db.UpdateEnterpriseUserProfile(
                        userId,
                        request.ManagerName ?? string.Empty,
                        request.ManagerContactNumber ?? string.Empty,
                        request.Email ?? string.Empty)
                    : db.UpdateStudentUserProfile(
                        userId,
                        request.Firstname ?? string.Empty,
                        request.Lastname ?? string.Empty,
                        request.StudentNumber ?? string.Empty,
                        request.Section ?? string.Empty,
                        birthdate,
                        request.Address ?? string.Empty,
                        request.ManagerContactNumber ?? string.Empty,
                        request.Email ?? string.Empty,
                        request.PhotoDataUrl ?? string.Empty);

                if (!updated)
                {
                    return Json(new { success = false, message = "Unable to save profile." });
                }

                Session.Remove(ProfileSessionKey);
                var refreshed = BuildUserProfileFromDatabase(userId);
                if (refreshed != null)
                {
                    Session[ProfileSessionKey] = refreshed;
                }

                return Json(new { success = true, message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("AccountSettings")]
        public ActionResult SaveAccountSettings([Bind(Prefix = "Profile")] UserProfileViewModel model)
        {
            PrepareProfilePage("AccountSettings", "none");

            if (string.IsNullOrWhiteSpace(model.PhotoDataUrl))
            {
                model.PhotoDataUrl = DefaultPhotoDataUrl();
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", BuildAccountSettingsViewModel(model, new ChangePasswordViewModel()));
            }

            Session[ProfileSessionKey] = model;
            TempData["FlashMessage"] = "Profile information saved.";
            return RedirectToAction("AccountSettings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword([Bind(Prefix = "PasswordChange")] ChangePasswordViewModel model)
        {
            PrepareProfilePage("Profile", "enterprises");

            var profile = GetProfile();
            var pageModel = BuildAccountSettingsViewModel(profile, model);
            pageModel.ReopenPasswordModal = true;

            if (!ModelState.IsValid)
            {
                return View("Profile", pageModel);
            }

            if (string.Equals(model.CurrentPassword, model.NewPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("PasswordChange.NewPassword", "New password must be different from the current password.");
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", pageModel);
            }

            TempData["FlashMessage"] = "Demo mode: password change validated successfully.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePasswordDemo([Bind(Prefix = "PasswordChange")] ChangePasswordViewModel model)
        {
            PrepareProfilePage("AccountSettings", "none");

            var profile = GetProfile();
            var pageModel = BuildAccountSettingsViewModel(profile, model);
            pageModel.ReopenPasswordModal = true;

            if (!ModelState.IsValid)
            {
                return View("Profile", pageModel);
            }

            if (string.Equals(model.CurrentPassword, model.NewPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("PasswordChange.NewPassword", "New password must be different from the current password.");
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", pageModel);
            }

            TempData["FlashMessage"] = "Demo mode: password change validated successfully.";
            return RedirectToAction("AccountSettings");
        }

        public ActionResult Transactions(string search = null)
        {
            ViewBag.PageClass = "page-transactions";
            ViewBag.HideGlobalHeader = true;
            ViewBag.NavSection = "none";

            var records = GetTransactions();
            if (!string.IsNullOrWhiteSpace(search))
            {
                records = records
                    .Where(record =>
                        record.UserName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        record.Product.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        record.StatusLabel.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            return View(new TransactionsPageViewModel
            {
                SearchTerm = search ?? string.Empty,
                Transactions = records
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            TempData["FlashMessage"] = "You have been logged out.";
            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.PageClass = "page-about";
            ViewBag.ShowBackButton = true;
            ViewBag.NavSection = "about";
            return View();
        }

        public ActionResult Contact()
        {
            return RedirectToAction("Transactions");
        }

        public ActionResult EnterpriseProfile()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                TempData["FlashMessage"] = "Please log in first.";
                return RedirectToAction("Login", "Home");
            }

            PrepareProfilePage("EnterpriseProfile", "enterprises");
            ViewBag.ShowProfileSaveActions = true;
            ViewBag.ShowProfileEditTools = true;
            ViewBag.Title = "Enterprise Profile";
            return View("EnterpriseProfile", BuildAccountSettingsViewModel(GetEnterpriseProfile(userId), new ChangePasswordViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveEnterpriseProfile([Bind(Prefix = "Profile")] UserProfileViewModel model, HttpPostedFileBase profileFile, HttpPostedFileBase qrFile)
        {
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                TempData["FlashMessage"] = "Please log in first.";
                return RedirectToAction("Login", "Home");
            }

            PrepareProfilePage("EnterpriseProfile", "enterprises");
            ViewBag.ShowProfileSaveActions = true;
            ViewBag.ShowProfileEditTools = true;
            ViewBag.Title = "Enterprise Profile";

            // Enterprise profile uses a shared model with student-specific validators.
            // Clear only unrelated validation entries so enterprise saves are not blocked.
            ModelState.Remove("Profile.ManagerName");

            if (!ModelState.IsValid)
            {
                TempData["FlashMessage"] = "Please complete required enterprise fields correctly.";
                return View("EnterpriseProfile", BuildAccountSettingsViewModel(model, new ChangePasswordViewModel()));
            }

            string logoPath = null;
            string qrPath = null;
            if (profileFile != null && profileFile.ContentLength > 0)
            {
                var uploadsFolder = Server.MapPath("~/Content/Uploads");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                var extension = System.IO.Path.GetExtension(profileFile.FileName);
                var safeExt = string.IsNullOrWhiteSpace(extension) ? ".png" : extension;
                var fileName = $"enterprise_{userId}_logo{safeExt}";
                var fullPath = System.IO.Path.Combine(uploadsFolder, fileName);
                profileFile.SaveAs(fullPath);
                logoPath = "/Content/Uploads/" + fileName;
            }

            byte[] qrBytes = null;
            if (qrFile != null && qrFile.ContentLength > 0)
            {
                var uploadsFolder = Server.MapPath("~/Content/Uploads");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }

                var qrExtension = System.IO.Path.GetExtension(qrFile.FileName);
                var qrSafeExt = string.IsNullOrWhiteSpace(qrExtension) ? ".png" : qrExtension;
                var qrFileName = $"enterprise_{userId}_qr{qrSafeExt}";
                var qrFullPath = System.IO.Path.Combine(uploadsFolder, qrFileName);
                qrFile.SaveAs(qrFullPath);
                qrPath = "/Content/Uploads/" + qrFileName;

                using (var br = new System.IO.BinaryReader(qrFile.InputStream))
                {
                    qrBytes = br.ReadBytes(qrFile.ContentLength);
                }
            }

            var ok = db.SaveEnterpriseProfileData(
                userId,
                model.EnterpriseName,
                model.EnterpriseType,
                model.Contact,
                model.Email,
                model.ManagerName,
                model.StudentId,
                model.ManagerContactNumber,
                model.Section,
                logoPath,
                qrBytes);

            if (!ok)
            {
                TempData["FlashMessage"] = "Unable to save enterprise profile.";
                return View("EnterpriseProfile", BuildAccountSettingsViewModel(model, new ChangePasswordViewModel()));
            }

            // Keep uploaded assets visible even if DB columns for these are not populated.
            if (!string.IsNullOrWhiteSpace(logoPath))
            {
                Session["EnterpriseLogoPath"] = logoPath;
            }
            if (!string.IsNullOrWhiteSpace(qrPath))
            {
                Session["EnterpriseQrPath"] = qrPath;
            }

            TempData["FlashMessage"] = "Enterprise profile saved.";
            return RedirectToAction("Index");
        }

        private void PrepareProfilePage(string settingsAction, string navSection)
        {
            ViewBag.PageClass = "page-profile";
            ViewBag.ShowBackButton = true;
            ViewBag.BackUrl = Url.Action("Index", "Home");
            ViewBag.SettingsAction = settingsAction;
            ViewBag.NavSection = navSection;
        }

        // ==================== UPDATED PREPAREUSERPROFILEPAGE METHOD ====================
        private void PrepareUserProfilePage()
        {
            ViewBag.PageClass = "page-profile";
            ViewBag.ShowBackButton = true;
            ViewBag.BackUrl = Url.Action("Index", "Home");
            ViewBag.NavSection = "none";
            ViewBag.ShowProfileEditTools = false;   // ADDED: This fixes the CS0103 error
            ViewBag.ShowProfileSaveActions = false;  // ADDED: For consistency
            ViewBag.Title = "User Profile";
        }
        // ==================== END OF UPDATED PREPAREUSERPROFILEPAGE ====================

        private AccountSettingsPageViewModel BuildAccountSettingsViewModel(UserProfileViewModel profile, ChangePasswordViewModel passwordChange)
        {
            return new AccountSettingsPageViewModel
            {
                Profile = profile,
                PasswordChange = passwordChange ?? new ChangePasswordViewModel()
            };
        }

        private EnterpriseDashboardViewModel BuildDashboardViewModel()
        {
            var userId = GetCurrentUserId();
            var enterpriseData = userId > 0 ? db.GetEnterpriseProfileDataByUserId(userId) : null;
            var statsData = userId > 0 ? db.GetEnterpriseDashboardStatsByUserId(userId) : new EnterpriseDashboardStatsData();
            var role = Session["Role"] == null ? "enterprise" : Session["Role"].ToString();
            var enterpriseName = enterpriseData == null ? string.Empty : (enterpriseData.StoreName ?? string.Empty);
            var enterpriseType = enterpriseData == null ? string.Empty : (enterpriseData.EnterpriseType ?? string.Empty);
            var enterpriseGcash = enterpriseData == null ? string.Empty : (enterpriseData.GcashNumber ?? string.Empty);
            var enterprisePhoto = enterpriseData == null || string.IsNullOrWhiteSpace(enterpriseData.StoreLogoPath)
                ? (ResolveUploadedAssetPath(userId, "logo") ?? (Session["EnterpriseLogoPath"] as string) ?? DefaultPhotoDataUrl())
                : enterpriseData.StoreLogoPath;
            var enterpriseQr = enterpriseData == null
                ? (ResolveUploadedAssetPath(userId, "qr") ?? (Session["EnterpriseQrPath"] as string) ?? string.Empty)
                : (!string.IsNullOrWhiteSpace(enterpriseData.QrDataUrl)
                    ? enterpriseData.QrDataUrl
                    : (ResolveUploadedAssetPath(userId, "qr") ?? (Session["EnterpriseQrPath"] as string) ?? string.Empty));

            return new EnterpriseDashboardViewModel
            {
                Enterprise = new EnterpriseSummary
                {
                    PhotoDataUrl = enterprisePhoto,
                    QrDataUrl = enterpriseQr,
                    Name = enterpriseName,
                    Type = enterpriseType,
                    Gcash = enterpriseGcash,
                    Role = role
                },
                Manager = new ManagerSummary
                {
                    Name = enterpriseData == null ? string.Empty : (enterpriseData.ManagerName ?? string.Empty),
                    Section = enterpriseData == null ? string.Empty : (enterpriseData.Section ?? string.Empty),
                    StudentId = enterpriseData == null ? string.Empty : (enterpriseData.ManagerStudentId ?? string.Empty),
                    ContactNumber = enterpriseData == null ? string.Empty : (enterpriseData.ManagerContact ?? string.Empty)
                },
                Stats = new EnterpriseStatsSummary
                {
                    OrdersCompleted = statsData.OrdersCompleted,
                    ProductsListed = statsData.ProductsListed,
                    TotalSales = statsData.TotalSales
                }
            };
        }

        private UserProfileViewModel GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId > 0)
            {
                var dbProfile = BuildUserProfileFromDatabase(userId);
                if (dbProfile != null)
                {
                    Session[ProfileSessionKey] = dbProfile;
                    return dbProfile;
                }
            }

            var profile = Session[ProfileSessionKey] as UserProfileViewModel;
            if (profile != null)
            {
                return profile;
            }

            profile = new UserProfileViewModel
            {
                PhotoDataUrl = DefaultPhotoDataUrl(),
                QrDataUrl = string.Empty,
                EnterpriseName = "Magic Powder",
                EnterpriseType = "Partnership",
                Contact = "09561234567",
                Email = "PabluEzkubr@gmail.com",
                ManagerName = "Pablo Escobar",
                StudentId = "24-6769",
                Section = "SBENT-3D",
                ManagerContactNumber = "09561234567"
            };

            Session[ProfileSessionKey] = profile;
            Session[PasswordSessionKey] = "bizzyqcu123";

            return profile;
        }

        private List<TransactionRecordViewModel> GetTransactions()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return new List<TransactionRecordViewModel>();
            }

            return db.GetTransactionHistoryByUserId(userId)
                .Select(record => new TransactionRecordViewModel
                {
                    UserName = string.IsNullOrWhiteSpace(record.CustomerName) ? "Customer" : record.CustomerName,
                    Product = record.Products,
                    Date = record.OrderDate,
                    Total = record.TotalAmount,
                    Status = string.IsNullOrWhiteSpace(record.Status) ? "preparing" : record.Status.Trim().ToLowerInvariant(),
                    StatusLabel = FormatTransactionStatus(record.Status)
                })
                .ToList();
        }

        private string FormatTransactionStatus(string status)
        {
            status = string.IsNullOrWhiteSpace(status) ? string.Empty : status.Trim();
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Preparing";
            }

            if (string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return "Completed";
            }

            if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "canceled", StringComparison.OrdinalIgnoreCase))
            {
                return "Cancelled";
            }

            return char.ToUpperInvariant(status[0]) + status.Substring(1).ToLowerInvariant();
        }

        private string DefaultPhotoDataUrl()
        {
            return "data:image/svg+xml;utf8," + HttpUtility.UrlEncode(
                "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 160 160'>" +
                "<rect width='160' height='160' rx='80' fill='#efefef'/>" +
                "<circle cx='80' cy='58' r='28' fill='#c9c9c9'/>" +
                "<path d='M39 136c7-24 23-38 41-38s34 14 41 38' fill='#c9c9c9'/>" +
                "</svg>");
        }

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null)
            {
                return 0;
            }

            int userId;
            return int.TryParse(Session["UserId"].ToString(), out userId) ? userId : 0;
        }

        private UserProfileViewModel BuildUserProfileFromDatabase(int userId)
        {
            var isEnterpriseUser = db.GetEnterpriseByUserId(userId) != null;
            if (isEnterpriseUser)
            {
                var enterpriseProfile = db.GetEnterpriseProfileDataByUserId(userId);
                if (enterpriseProfile != null)
                {
                    return new UserProfileViewModel
                    {
                        PhotoDataUrl = string.IsNullOrWhiteSpace(enterpriseProfile.StoreLogoPath)
                            ? (ResolveUploadedAssetPath(userId, "logo") ?? (Session["EnterpriseLogoPath"] as string) ?? DefaultPhotoDataUrl())
                            : enterpriseProfile.StoreLogoPath,
                        QrDataUrl = !string.IsNullOrWhiteSpace(enterpriseProfile.QrDataUrl)
                            ? enterpriseProfile.QrDataUrl
                            : (ResolveUploadedAssetPath(userId, "qr") ?? (Session["EnterpriseQrPath"] as string) ?? string.Empty),
                        EnterpriseName = enterpriseProfile.StoreName ?? string.Empty,
                        EnterpriseType = enterpriseProfile.EnterpriseType ?? string.Empty,
                        Contact = enterpriseProfile.GcashNumber ?? string.Empty,
                        Email = enterpriseProfile.Email ?? string.Empty,
                        ManagerName = enterpriseProfile.ManagerName ?? string.Empty,
                        StudentId = enterpriseProfile.ManagerStudentId ?? string.Empty,
                        Section = enterpriseProfile.Section ?? string.Empty,
                        ManagerContactNumber = enterpriseProfile.ManagerContact ?? string.Empty
                    };
                }
            }

            var data = isEnterpriseUser
                ? db.GetEnterpriseUserProfileByUserId(userId)
                : db.GetStudentUserProfileByUserId(userId);
            if (data == null)
            {
                return null;
            }

            return new UserProfileViewModel
            {
                PhotoDataUrl = string.IsNullOrWhiteSpace(data.PhotoDataUrl) ? DefaultPhotoDataUrl() : data.PhotoDataUrl,
                QrDataUrl = string.Empty,
                EnterpriseName = string.Empty,
                EnterpriseType = string.Empty,
                Contact = data.ContactNumber ?? string.Empty,
                Email = data.Email ?? string.Empty,
                ManagerName = data.Name ?? string.Empty,
                StudentId = data.StudentNumber ?? string.Empty,
                Section = data.Section ?? string.Empty,
                ManagerContactNumber = data.ContactNumber ?? string.Empty,
                Firstname = data.Firstname ?? string.Empty,
                Lastname = data.Lastname ?? string.Empty,
                Birthdate = data.Birthdate.HasValue ? data.Birthdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                Address = data.Address ?? string.Empty
            };
        }

        private UserProfileViewModel GetEnterpriseProfile(int userId)
        {
            var data = db.GetEnterpriseProfileDataByUserId(userId);
            if (data == null)
            {
                return new UserProfileViewModel
                {
                    PhotoDataUrl = DefaultPhotoDataUrl(),
                    QrDataUrl = string.Empty,
                    EnterpriseName = string.Empty,
                    EnterpriseType = string.Empty,
                    Contact = string.Empty,
                    Email = string.Empty,
                    ManagerName = string.Empty,
                    StudentId = string.Empty,
                    Section = string.Empty,
                    ManagerContactNumber = string.Empty
                };
            }

            return new UserProfileViewModel
            {
                PhotoDataUrl = string.IsNullOrWhiteSpace(data.StoreLogoPath)
                    ? (ResolveUploadedAssetPath(userId, "logo") ?? (Session["EnterpriseLogoPath"] as string) ?? DefaultPhotoDataUrl())
                    : data.StoreLogoPath,
                QrDataUrl = !string.IsNullOrWhiteSpace(data.QrDataUrl)
                    ? data.QrDataUrl
                    : (ResolveUploadedAssetPath(userId, "qr") ?? (Session["EnterpriseQrPath"] as string) ?? string.Empty),
                EnterpriseName = data.StoreName ?? string.Empty,
                EnterpriseType = data.EnterpriseType ?? string.Empty,
                Contact = data.GcashNumber ?? string.Empty,
                Email = data.Email ?? string.Empty,
                ManagerName = data.ManagerName ?? string.Empty,
                StudentId = data.ManagerStudentId ?? string.Empty,
                Section = data.Section ?? string.Empty,
                ManagerContactNumber = data.ManagerContact ?? string.Empty
            };
        }

        private string ResolveUploadedAssetPath(int userId, string assetType)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(assetType))
            {
                return null;
            }

            try
            {
                var uploadsFolder = Server.MapPath("~/Content/Uploads");
                if (string.IsNullOrWhiteSpace(uploadsFolder) || !System.IO.Directory.Exists(uploadsFolder))
                {
                    return null;
                }

                var pattern = $"enterprise_{userId}_{assetType}.*";
                var files = System.IO.Directory.GetFiles(uploadsFolder, pattern);
                if (files == null || files.Length == 0)
                {
                    return null;
                }

                var latest = files
                    .Select(path => new System.IO.FileInfo(path))
                    .OrderByDescending(info => info.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latest == null)
                {
                    return null;
                }

                return "/Content/Uploads/" + latest.Name;
            }
            catch
            {
                return null;
            }
        }
    }
}
