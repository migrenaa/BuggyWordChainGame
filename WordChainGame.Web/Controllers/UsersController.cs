

namespace WordChainGame.Web.Controllers
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Cookies;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using WordChainGame.Common.Enumerations;
    using WordChainGame.Data.Entities;
    using WordChainGame.Services.Services;
    using WordChainGame.Web.Models;

    [Authorize]
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private ApplicationUserManager _userManager;
        private IUserService users;

        public UsersController(IUserService users)
        {
            this.users = users;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
            
        // POST api/Account/Logout
        [Route("Logout")]
        public async Task<IHttpActionResult> Logout()
        {
            Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie); 
            return Ok();
        }
        

        /// <summary>
        /// Deletes user with his user info. There is validation by password.
        /// </summary>
        /// <param name="password">The password</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete]
        [Route("")]
        public async Task<IHttpActionResult> DeleteAccount(string password)
        {
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);

            if(user.Roles.Select(x => x.RoleId).Contains(((int)UserRole.Admin).ToString()))
            {
                return BadRequest("The admin cannot be deleted.");
            }

            bool isPasswordValid = await UserManager.CheckPasswordAsync(user, password);

            if(!isPasswordValid)
            {
                return Unauthorized();
            }

            users.DeleteUserEntities(userId);

            var result = await UserManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();

        }


      
        /// <summary>
        /// Registers user in the system. 
        /// </summary>
        /// <param name="model">The model with the information with the user.</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User { UserName = model.UserName, Email = model.Email, FullName = model.FullName };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            var adminRoleId = (int)UserRole.Admin;
            var admin = this.UserManager.Users.SingleOrDefault(x => x.Roles.Select(r => r.RoleId).Contains(adminRoleId.ToString()));

            if(admin == null)
            {
                await UserManager.AddToRoleAsync(user.Id, UserRole.Admin.ToString());
            }
            else
            {
                await UserManager.AddToRoleAsync(user.Id, UserRole.User.ToString());
            }
            
            return Ok(user);
        }

       

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
