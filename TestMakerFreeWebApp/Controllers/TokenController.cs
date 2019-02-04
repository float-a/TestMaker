using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using TestMakerFreeWebApp.Data;
using TestMakerFreeWebApp.Data.Models;
using TestMakerFreeWebApp.ViewModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TestMakerFreeWebApp.Controllers
{
    public class TokenController : BaseApiController
    {

        public TokenController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration) : base(context, roleManager, userManager, configuration)
        {
            SignInManager = signInManager;
        }

        protected SignInManager<ApplicationUser> SignInManager { get; private set; }

        [HttpPost("Auth")]
        public async Task<IActionResult> Jwt([FromBody] TokenRequestViewModel model)
        {
            if (model == null) return new StatusCodeResult(500);

            switch(model.grant_type)
            {
                case "password":
                    return await GetToken(model);
                case "refresh_token":
                    return await RefreshToken(model);
                default:
                    return new UnauthorizedResult();
            }
        }

        private async Task<IActionResult> GetToken(TokenRequestViewModel model)
        {
            try
            {
                var user = await UserManager.FindByNameAsync(model.username);
                if(user == null && model.username.Contains("@"))
                {
                    user = await UserManager.FindByEmailAsync(model.username);
                }

                if(user == null || !await UserManager.CheckPasswordAsync(user, model.password))
                {
                    // user does not exists or password mismatch
                    return new UnauthorizedResult();
                }

                //username & password matches: create and return Jwt token
                var rt = CreateRefreshToken(model.client_id, user.Id);

                DbContext.Tokens.Add(rt);
                DbContext.SaveChanges();

                var t = CreateAccessToken(user.Id, rt.Value);

                return Json(t);
            }
            catch(Exception ex)
            {
                return new UnauthorizedResult();
            }
        }

        private async Task<IActionResult> RefreshToken(TokenRequestViewModel model)
        {
            try
            {
                var rt = DbContext.Tokens.FirstOrDefault(t => t.ClientId == model.client_id && t.Value == model.refresh_token);

                if(rt == null)
                {
                    return new UnauthorizedResult();
                }

                var user = await UserManager.FindByIdAsync(rt.UserId);
                if(user == null)
                {
                    return new UnauthorizedResult();
                }

                // generate a new refresh token
                var rtNew = CreateRefreshToken(rt.ClientId, rt.UserId);

                // invalidate the old refresh token
                DbContext.Tokens.Remove(rt);

                // add the new refresh token
                DbContext.Tokens.Add(rtNew);

                // persist the changes in DB
                DbContext.SaveChanges();

                // create a new access token
                var response = CreateAccessToken(rtNew.UserId, rtNew.Value);

                return Json(response);
            }
            catch (Exception ex)
            {
                return new UnauthorizedResult();
            }
        }

        private Token CreateRefreshToken(string clientId, string userId)
        {
            return new Token()
            {
                ClientId = clientId,
                UserId = userId,
                Type = 0,
                Value = Guid.NewGuid().ToString("N"),
                CreatedDate = DateTime.UtcNow
            };
        }

        private TokenResponseViewModel CreateAccessToken(string userId, string refreshToken)
        {
            DateTime now = DateTime.UtcNow;

            // add the registered claims for JWT (RFC7519)

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString())
                // TODO: add additional claims here
            };

            var tokenExpirationMins = Configuration.GetValue<int>("Auth:Jwt:TokenExpirationInMinutes");
            var issuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Auth:Jwt:key"]));

            var token = new JwtSecurityToken(
                issuer: Configuration["Auth:Jwt:Issuer"],
                audience: Configuration["Auth:Jwt:Audience"],
                claims: claims,
                notBefore: now,
                expires: now.Add(TimeSpan.FromMinutes(tokenExpirationMins)),
                signingCredentials: new SigningCredentials(issuerSigningKey, SecurityAlgorithms.HmacSha256)
             );
            var encodedToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResponseViewModel()
            {
                token = encodedToken,
                expiration = tokenExpirationMins,
                refresh_token = refreshToken
            };
        }

        [HttpPost("Facebook")]
        public async Task<IActionResult> Facebook([FromBody] ExternalLoginRequestViewModel model)
        {
            try
            {
                var fbAPI_url = "https://graph.facebook.com/v2.10/";
                var fbAPI_queryString = String.Format("me?scope=email&access_token={0}&fields=id,name,email", model.access_token);
                string result = null;

                // fetch the user info from Facebook Graph v2.10
                using (var c = new HttpClient())
                {
                    c.BaseAddress = new Uri(fbAPI_url);
                    var response = await c.GetAsync(fbAPI_queryString);
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                    else throw new Exception("Authentication error");
                };

                //load the resulting Json into a dictionary
                var epInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
                var info = new UserLoginInfo("facebook", epInfo["id"], "Facebook");

                //Check if this user already registered himself with this external provider before
                var user = await UserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user == null)
                {
                    // if we reach this point, it means that this user never tried to logged in 
                    // using this external provider. However, it could have used other providers and/or have a local account
                    // We can find out if that's the case by looking for his e-mail address

                    user = await UserManager.FindByEmailAsync(epInfo["email"]);
                    if (user == null)
                    {
                        DateTime now = DateTime.Now;
                        var username = String.Format("FB{0}{1}", epInfo["id"], Guid.NewGuid().ToString("N"));
                        user = new ApplicationUser()
                        {
                            SecurityStamp = Guid.NewGuid().ToString(),
                            UserName = username, // ensure the user will have an unique username
                            Email = epInfo["email"],
                            DisplayName = epInfo["name"],
                            CreatedDate = now,
                            LastModifiedDate = now
                        };

                        //Add the user to the Db with a random password
                        await UserManager.CreateAsync(user, DataHelper.GenerateRandomPassword());

                        await UserManager.AddToRoleAsync(user, "RegisteredUser");
                        user.EmailConfirmed = true;
                        user.LockoutEnabled = false;
                        DbContext.SaveChanges();
                    }
                    //Register this external provider to the user
                    var ir = await UserManager.AddLoginAsync(user, info);
                    if (ir.Succeeded)
                    {
                        // Persist everything into the Db
                        DbContext.SaveChanges();
                    }
                    else throw new Exception("Authentication error");
                }
                    //create the refresh token
                    var rt = CreateRefreshToken(model.client_id, user.Id);

                    DbContext.Tokens.Add(rt);
                    DbContext.SaveChanges();

                    var t = CreateAccessToken(user.Id, rt.Value);
                    return Json(t);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
            
        [HttpGet("ExternalLogin/{provider}")]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            switch(provider.ToLower())
            {
                case "facebook":
                    var redirectUrl = Url.Action(nameof(ExternalLoginCallBack), "Token", new { returnUrl });
                    var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                    return Challenge(properties, provider);
                default:
                    //provider not supported
                    return BadRequest(new
                    {
                        Error = String.Format("Provider '{0}' is not supported.", provider)
                    });
            }
        }

        [HttpGet("ExternalLoginCallback")]
        public async Task<IActionResult> ExternalLoginCallBack(string returnUrl = null, string remoteError = null)
        {
            if(!String.IsNullOrEmpty(remoteError))
            {
                throw new Exception(String.Format("External Provider error: {0}", remoteError));
            }

            var info = await SignInManager.GetExternalLoginInfoAsync();
            if(info == null)
            {
                throw new Exception("ERROR: No login info available");
            }
            var user = await UserManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if(user == null)
            {
                var emailKey = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
                var email = info.Principal.FindFirst(emailKey).Value;
                user = await UserManager.FindByEmailAsync(email);
                if(user == null)
                {
                    DateTime now = DateTime.Now;

                    var idKey = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
                    var username = String.Format("{0}{1}{2}", info.LoginProvider, info.Principal.FindFirst(idKey).Value, Guid.NewGuid().ToString("N"));

                    user = new ApplicationUser()
                    {
                        SecurityStamp = Guid.NewGuid().ToString(),
                        UserName = username,
                        Email = email,
                        CreatedDate = now,
                        LastModifiedDate = now
                    };

                    await UserManager.CreateAsync(user, DataHelper.GenerateRandomPassword());
                    await UserManager.AddToRoleAsync(user, "RegisteredUser");

                    user.EmailConfirmed = true;
                    user.LockoutEnabled = true;

                    await DbContext.SaveChangesAsync();
                }
                var ir = await UserManager.AddLoginAsync(user, info);
                if (ir.Succeeded)
                {
                    DbContext.SaveChanges();
                }
                else throw new Exception("Authentication error");
            }

            var rt = CreateRefreshToken("TestMakerFree", user.Id);
            DbContext.Tokens.Add(rt);
            DbContext.SaveChanges();

            var t = CreateAccessToken(user.Id, rt.Value);

            return Content("<script type=\"text/javascript\">" +
                "window.opener.externalProviderLogin(" +
                JsonConvert.SerializeObject(t, JsonSettings) +
                ");" +
                "window.close();" +
                "</script>",
                "text/html");
        }
    }
}

