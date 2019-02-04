using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMakerFreeWebApp.Data;
using TestMakerFreeWebApp.Data.Models;
using TestMakerFreeWebApp.ViewModels;

namespace TestMakerFreeWebApp.Controllers
{
    public class ResultController : BaseApiController
    {

        public ResultController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IConfiguration configuration) : base(context, roleManager, userManager, configuration) { }

        #region RESTful conventions methods
        [HttpGet("{id})")]
        public IActionResult Get(int id)
        {
            var result = DbContext.Results.Where(i => i.Id == id).FirstOrDefault();
            if(result == null)
            {
                return NotFound(new
                {
                    Error = String.Format("Result ID {0} has not been found", id)
                });
            }

            return new JsonResult(result.Adapt<ResultViewModel>(),
                JsonSettings);
        }

        [HttpPut]
        [Authorize]
        public IActionResult Put([FromBody]ResultViewModel model)
        {
            if (model == null) return new StatusCodeResult(500);

            var result = model.Adapt<Result>();

            result.CreatedDate = DateTime.Now;
            result.LastModifiedDate = result.CreatedDate;
            DbContext.Results.Add(result);
            DbContext.SaveChanges();

            return new JsonResult(result.Adapt<ResultViewModel>(),
                JsonSettings);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Post([FromBody]ResultViewModel model)
        {
            if (model == null) return new StatusCodeResult(500);

            var result = DbContext.Results.Where(q => q.Id == model.Id).FirstOrDefault();

            if(result == null)
            {
                return NotFound(new
                {
                    Error = String.Format("Result ID {0} has not been found", model.Id)
                });
            }

            result.QuizId = model.QuizId;
            result.Text = model.Text;
            result.MinValue = model.MinValue;
            result.MaxValue = model.MaxValue;
            result.Notes = model.Notes;
            result.LastModifiedDate = result.CreatedDate;

            DbContext.SaveChanges();

            return new JsonResult(result.Adapt<ResultViewModel>(),
                JsonSettings);

        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var result = DbContext.Results.Where(i => i.Id == id).FirstOrDefault();

            if (result == null)
            {
                return NotFound(new
                {
                    Error = String.Format("Result ID {0} has not been found", id)
                });
            }

            DbContext.Results.Remove(result);
            DbContext.SaveChanges();

            return new OkResult();
        }
        #endregion

        [HttpGet("All/{quizId:int}")]
        public IActionResult All(int quizId)
        {
            var results = DbContext.Results.Where(q => q.QuizId == quizId).ToArray();

            return new JsonResult(results.Adapt<ResultViewModel[]>(),
                JsonSettings);
        }
    }
}
