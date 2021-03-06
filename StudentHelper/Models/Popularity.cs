﻿using Newtonsoft.Json;
using StudentHelper.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace StudentHelper.Models
{
    public class Popularity
    {
        [Key, Column(Order = 0)]
        public int CourseId { get; set; }
        [Key, Column(Order = 1)]
        public int UserDetailsId { get; set; }
        [JsonIgnore]
        public virtual Course Course { get; set; }
        public virtual UserDetails UserDetails { get; set; }
        public int Votes { get; set; }

        public static void PostLiked(int courseId, int userId, StudentHelperContext db)
        {
            ChangeStats(courseId, userId, db, 3);
        }

        public static void PostDisliked(int courseId, int userId, StudentHelperContext db)
        {
            ChangeStats(courseId, userId, db, -3);
        }

        public static void CommentLiked(int courseId, int userId, StudentHelperContext db)
        {
            ChangeStats(courseId, userId, db, 1);
        }

        public static void CommentDisliked(int courseId, int userId, StudentHelperContext db)
        {
            ChangeStats(courseId, userId, db, -1);
        }

        private static void ChangeStats(int courseId, int userId, StudentHelperContext db, int votes)
        {
            Course course = db.Courses.Find(courseId);
            Popularity popularity = course.PopularityStats.First(p => p.UserDetailsId == userId);
            popularity.Votes = popularity.Votes + votes;
        }

    }
}