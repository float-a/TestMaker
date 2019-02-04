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
    public class QuestionController : BaseApiController
    {

        public QuestionController(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IConfiguration configuration) : base(context, roleManager, userManager, configuration) { }

        #region RESTful conventions methods
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var question = DbContext.Questions.Where(i => i.Id == id).FirstOrDefault();

            if(question == null)
            {
                return NotFound(new
                {
                    Error = String.Format("Question ID {0} has not been found", id)
                });
            }

            return new JsonResult(
                question.Adapt<QuestionViewModel>(),
                JsonSettings);
        }

        [HttpPut]
        [Authorize]
        public IActionResult Put([FromBody]QuestionViewModel model)
        {
            if (model == null) return new StatusCodeResult(500);
            var question = model.Adapt<Question>();
            question.QuizId = model.QuizId;
            question.Text = model.Text;
            question.Notes = model.Notes;
            question.CreatedDate = model.CreatedDate;
            question.LastModifiedDate = model.LastModifiedDate;

            DbContext.Questions.Add(question);
            DbContext.SaveChanges();

            return new JsonResult(question.Adapt<QuestionViewModel>(), JsonSettings);

        }

        [HttpPost]
        [Authorize]
        public IActionResult Post([FromBody]QuestionViewModel model)
        {
            if (model == null) return new StatusCodeResult(500);
            var question = DbContext.Questions.Where(q => q.Id == model.Id).FirstOrDefault();

            if(question == null)
            {
                return NotFound(new
                {
                    Error = String.Format("Question ID {0} has not been found", model.Id)
                });
            }

            question.QuizId = model.QuizId;
            question.Text = model.Text;
            question.Notes = model.Notes;

            question.LastModifiedDate = question.CreatedDate;
            DbContext.SaveChanges();

            return new JsonResult(question.Adapt<QuestionViewModel>(), JsonSettings);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var question = DbContext.Questions.Where(q => q.Id == id).FirstOrDefault();
            if(question == null)
            {
                return NotFound(new
                {
                    Error = String.Format("Question ID {0} has not been found", id)
                });
            }
            DbContext.Questions.Remove(question);
            DbContext.SaveChanges();

            return new OkResult();
        }
        #endregion

        [HttpGet("All/{quizId:int}")]
        public IActionResult All(int quizId)
        {
            HttpContext.Response.Headers["Content-Type"] = "application/json";
            var questions = DbContext.Questions.Where(q => q.QuizId == quizId).ToArray();

            return new JsonResult(
                questions.Adapt<QuestionViewModel[]>(),
                JsonSettings);
        }
    }
}
