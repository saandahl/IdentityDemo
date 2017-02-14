using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using TestSite.Models.Account;
using Microsoft.AspNet.Identity.EntityFramework;
using PagedList;


namespace TestSite.Controllers
{
    [EPiServer.PlugIn.GuiPlugIn(Area = EPiServer.PlugIn.PlugInArea.AdminMenu, Url = "/Identity/Index", DisplayName = "Identity Management")]
    public class IdentityController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        // Set how large the Account overview tabel should be.
        const int _PageSize = 2;

        public IdentityController()
        {
        }

        public IdentityController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ActionResult Index(string currentFilter, string searchString, int? page)
        {
            int pageNumber = (page ?? 1);
            var users = _db.Users.ToList();
            ViewBag.TotalCount = users.Count;

            if (string.IsNullOrEmpty(searchString) && (page == null || page == pageNumber))
            {
                return View(users.ToPagedList(pageNumber, _PageSize));
            }

            if (!string.IsNullOrEmpty(currentFilter))
                searchString = currentFilter;

            ViewBag.CurrentFilter = searchString;
            users = _db.Users.Where(s => s.UserName.Contains(searchString)).ToList();

            return View(users.ToPagedList(pageNumber, _PageSize));
        }

        public ActionResult EditUser(string id, int? page, string searchString)
        {
            if (id == null)
            {
                return HttpNotFound();
            }
            ApplicationUser user = _db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            return PartialView(user);
        }

        [HttpPost]
        public ActionResult EditUser(ApplicationUser user, int? page, string searchString, string password)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(user).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index", new { page = page, searchString = searchString });
            }
            return PartialView(user);
        }

        public ActionResult ChangePassword(string id, int? page, string searchString)
        {
            ChangePasswordViewModel model = new ChangePasswordViewModel();
            if (id == null)
            {
                ViewBag.ResultMessage = "User don't exist!";
                return PartialView(model);
            }
            ApplicationUser user = _db.Users.Find(id);
            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            ViewBag.Id = id;
            ViewBag.UserName = user.UserName;
            return View(model);
        }

        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordViewModel model, int? page, string searchString, string id)
        {
            ApplicationUser user = _db.Users.Find(id);

            if (user == null)
            {
                ViewBag.ResultMessage = "User doesn't exist!";
                return PartialView(model);
            }

            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            ViewBag.Id = id;
            ViewBag.UserName = user.UserName;

            if (ModelState.IsValid)
            {
                var isNewPasswordValid = UserManager.PasswordValidator.ValidateAsync(model.NewPassword).Result;
                if (isNewPasswordValid.Succeeded)
                {
                    if (UserManager.HasPassword(user.Id))
                        UserManager.RemovePassword(user.Id);


                    var result = UserManager.AddPassword(user.Id, model.NewPassword);

                    if (result.Succeeded)
                    {
                        _db.Users.AddOrUpdate(user);
                        _db.SaveChanges();
                        ViewBag.StatusMessage = "User updated!";

                        return RedirectToAction("Index", new { page = page, searchString = searchString });
                    }
                    AddErrors(result);
                }
                else
                {
                    AddErrors(isNewPasswordValid);
                }
                
            }
            return PartialView(model);
        }


        public ActionResult DeleteUser(string id, int? page, string searchString)
        {
            if (id == null)
            {
                return HttpNotFound();
            }
            ApplicationUser user = _db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.Page = page;
            ViewBag.SearchString = searchString;
            return PartialView(user);
        }

        [HttpPost]
        public ActionResult DeleteUser(ApplicationUser user, int? page, string searchString)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(user).State = EntityState.Deleted;
                _db.Users.Remove(user);
                _db.SaveChanges();
                ViewBag.Page = page;
                ViewBag.SearchString = searchString;
                return RedirectToAction("Index", new { page = page, searchString = searchString });
            }
            return View(user);
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser()
                {
                    UserName = model.UserName,
                    Email = model.UserName
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    ViewBag.StatusMessage = "User Created!";
                    return RedirectToAction("Index");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        // Roles Functions //////////////////////////////////////////////////////

        [HttpPost]
        public ActionResult ManageRoles(FormCollection collection)
        {
            try
            {
                _db.Roles.Add(new IdentityRole()
                {
                    Name = collection["RoleName"]
                });
                _db.SaveChanges();
                ViewBag.ResultMessage = "Role created successfully !";
                var roles = _db.Roles.ToList();
                return View(roles);
            }
            catch (Exception)
            {
                var roles = _db.Roles.ToList();
                return View(roles);
            }
        }

        [HttpGet]
        public ActionResult ManageRoles()
        {
            var roles = _db.Roles.ToList();
            return View(roles);
        }

        public ActionResult DeleteRole(string roleName)
        {
            var thisRole = _db.Roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.CurrentCultureIgnoreCase));
            _db.Roles.Remove(thisRole);
            _db.SaveChanges();
            var roles = _db.Roles.ToList();
            return RedirectToAction("ManageRoles", roles);
        }

        public ActionResult EditRole(string roleName)
        {
            var thisRole = _db.Roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.CurrentCultureIgnoreCase));
            return PartialView(thisRole);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRole(IdentityRole role)
        {
            try
            {
                _db.Entry(role).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();
                var roles = _db.Roles.ToList();
                return RedirectToAction("ManageRoles", roles);
            }
            catch
            {
                return RedirectToAction("ManageRoles");
            }
        }

        public ActionResult ManageUserRoles(string id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }
            ApplicationUser user = _db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            ViewBag.RolesForThisUser = UserManager.GetRoles(user.Id);
            ViewBag.UserName = user.UserName;

            // prepopulat roles for the view dropdown
            var list = _db.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RoleAddToUser(string userName, string roleName)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));

            if (user == null)
            {
                ViewBag.ResultMessage = "User don't exist!";
                return View("ManageUserRoles");
            }

            UserManager.AddToRole(user.Id, roleName);
            _db.Users.AddOrUpdate(user);
            _db.SaveChanges();
            ViewBag.ResultMessage = "Role created successfully !";

            // prepopulat roles for the view dropdown
            var list = _db.Roles.OrderBy(r => r.Name).ToList().Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name }).ToList();
            ViewBag.Roles = list;

            return RedirectToAction("ManageUserRoles", new { id = user.Id });

        }

        public ActionResult DeleteRoleForUser(string userName, string roleName)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.role = roleName;
            return PartialView(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRoleForUser(string id, string userName, string roleName)
        {
            var user =
                _db.Users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.CurrentCultureIgnoreCase));
            if (user == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {

                if (UserManager.IsInRole(user.Id, roleName))
                {
                    UserManager.RemoveFromRole(user.Id, roleName);
                    ViewBag.ResultMessage = "Role removed from this user successfully !";
                }
                else
                {
                    ViewBag.ResultMessage = "This user doesn't belong to selected role.";
                }
                ViewBag.RolesForThisUser = UserManager.GetRoles(user.Id);
                ViewBag.UserName = user.UserName;
                // prepopulat roles for the view dropdown
                var list =
                    _db.Roles.OrderBy(r => r.Name)
                        .ToList()
                        .Select(rr => new SelectListItem { Value = rr.Name.ToString(), Text = rr.Name })
                        .ToList();
                ViewBag.Roles = list;

                return RedirectToAction("ManageUserRoles", new { id = user.Id });
            }
            return RedirectToAction("ManageUserRoles", new { id = user.Id });
        }
    }
}
