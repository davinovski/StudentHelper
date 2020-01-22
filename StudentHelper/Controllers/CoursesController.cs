﻿using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using Newtonsoft.Json;
using StudentHelper.Data;
using StudentHelper.Models;
using StudentHelper.Models.DTOs;
using StudentHelper.Models.Pagination;

namespace StudentHelper.Controllers
{
    public class CoursesController : ApiController
    {
        private StudentHelperContext db = new StudentHelperContext();

        // GET: api/Courses?page=1&pageSize=10&year=1&year=2&semester=zimski
        public IHttpActionResult GetCourses([FromUri] CourseFilter courseFilter, int page = 1, int pageSize = 12)
        {
            var queryable = Course.FilterCourses(db.Courses, courseFilter);
            var coursesPage = Pagination.CreateMappedPage<Course, CourseCard>(
                queryable, page, pageSize, "Title", true
            );
            return Ok(coursesPage);
        }

        // GET: api/Courses/Search?searchTerm=term
        [Route("api/courses/search")]
        public IHttpActionResult GetCoursesBySearch(string searchTerm, int page = 1, int pageSize = 12)
        {
            var queryable = Course.SearchCourses(db.Courses, searchTerm);
            var coursesPage = Pagination.CreateMappedPage<Course, CourseCard>(
                queryable, page, pageSize, "Title", true
            );
            return Ok(coursesPage);
        }

        // GET: api/Courses/5
        public IHttpActionResult GetCourseDetails([FromUri] int id)
        {
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Format("Course with ID = {0} doesn't exists", id))
                };
                throw new HttpResponseException(resp);
            }
            course.PopularityStats = course.PopularityStats.OrderByDescending(ps => ps.Votes).Take(5).ToList();
            return Ok(course);
        }

        // POST: api/Courses
        public async Task<IHttpActionResult> PostCourse()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }
            var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();

            var jsonCourse = await filesReadToProvider.Contents[0].ReadAsStringAsync();
            Course course = JsonConvert.DeserializeObject<Course>(jsonCourse, new CourseJsonConverter() { DbContext = db });
            var imageBytes = await filesReadToProvider.Contents[1].ReadAsByteArrayAsync();

            course.ImageUrl = ImageController.SaveImage(imageBytes, Request, db);
            db.Courses.Add(course);
            db.SaveChanges();

            return Created(Request.RequestUri.ToString() + "/" + course.Id, course);
        }

        // DELETE: api/Courses/5
        public IHttpActionResult DeleteCourse(int id)
        {
            Course course = db.Courses.Find(id);
            if (course != null)
            {
                int? oldImageId = Image.ExtractImageId(course.ImageUrl);
                if (oldImageId != null)
                {
                    ImageController.DeleteImage(oldImageId.Value, db);
                }
                db.Courses.Remove(course);
                db.SaveChanges();
            }
            return Ok();
        }

        // PUT: api/Courses/5
        public IHttpActionResult PutCourse(int id, EditCourse editCourseRequest)
        {
            if (id != editCourseRequest.Id || !CourseExists(id))
            {
                return BadRequest();
            }

            Course course = db.Courses.Find(id);

            var configuration = new MapperConfiguration(cfg => cfg.CreateMap<EditCourse, Course>());
            new Mapper(configuration).Map<EditCourse, Course>(editCourseRequest, course);

            course.Professors = course.Professors.Where(p => editCourseRequest.ProfessorIds.Contains(p.Id)).ToList();
            editCourseRequest.ProfessorIds
                .Where(pId => !course.Professors.Select(p => p.Id).Contains(pId))
                .Select(pId => db.Staffs.Find(pId))
                .ToList()
                .ForEach(proffessor => course.Professors.Add(proffessor));

            course.Assistants = course.Assistants.Where(a => editCourseRequest.AssistantIds.Contains(a.Id)).ToList();
            editCourseRequest.AssistantIds
                .Where(aId => !course.Assistants.Select(a => a.Id).Contains(aId))
                .Select(aId => db.Staffs.Find(aId))
                .ToList()
                .ForEach(assistant => course.Assistants.Add(assistant));

            db.SaveChanges();

            return Ok(course);
        }

        // PATCH: api/Courses/ChangeImage/5
        [Route("api/courses/changeImage/{id}")]
        public async Task<IHttpActionResult> PatchCourse(int id)
        {
            if (!CourseExists(id))
            {
                return BadRequest();
            }
            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            Course course = db.Courses.Find(id);
            var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();
            var imageBytes = await filesReadToProvider.Contents[0].ReadAsByteArrayAsync();
            
            int? oldImageId = Image.ExtractImageId(course.ImageUrl);

            if (oldImageId != null)
            {
                ImageController.DeleteImage(oldImageId.Value, db);
            }
            course.ImageUrl = ImageController.SaveImage(imageBytes, Request, db);

            db.SaveChanges();

            return Ok(course);
        }        

        private bool CourseExists(int id)
        {
            return db.Courses.Count(e => e.Id == id) > 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}