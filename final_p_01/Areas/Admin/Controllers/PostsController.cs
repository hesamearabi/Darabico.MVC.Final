using System;
using System.IO;
using System.Linq;
using final_p_01.Domain.Entities;
using final_p_01.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace final_p_01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var posts = _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return View(posts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Post post, IFormFile? coverImage)
        {
            ModelState.Remove("Category");
            ModelState.Remove("CoverImagePath");
            ModelState.Remove("Faqs");

            if (ModelState.IsValid)
            {
                bool isSlugExist = _context.Posts.Any(p => p.Slug == post.Slug);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً برای مقاله دیگری ثبت شده است. لطفاً کلمه دیگری اضافه کنید.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                if (coverImage != null && coverImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + coverImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        coverImage.CopyTo(fileStream);
                    }
                    post.CoverImagePath = "/uploads/posts/" + uniqueFileName;
                }

                post.CreatedAt = DateTime.Now;
                _context.Posts.Add(post);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var post = _context.Posts
                .Include(p => p.Faqs)
                .FirstOrDefault(p => p.Id == id);

            if (post == null) return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Post post, IFormFile? coverImage)
        {
            if (id != post.Id) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("CoverImagePath");
            ModelState.Remove("Faqs");

            if (ModelState.IsValid)
            {
                bool isSlugExist = _context.Posts.Any(p => p.Slug == post.Slug && p.Id != post.Id);
                if (isSlugExist)
                {
                    ModelState.AddModelError("Slug", "این آدرس (URL) قبلاً برای مقاله دیگری ثبت شده است. لطفاً آن را تغییر دهید.");
                    ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
                    return View(post);
                }

                var existingPost = _context.Posts.Include(p => p.Faqs).FirstOrDefault(p => p.Id == id);
                if (existingPost == null) return NotFound();

                existingPost.Title = post.Title;
                existingPost.Slug = post.Slug;
                existingPost.Content = post.Content;
                existingPost.CategoryId = post.CategoryId;
                existingPost.MetaDescription = post.MetaDescription;
                existingPost.IsPublished = post.IsPublished;

                if (coverImage != null && coverImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + coverImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        coverImage.CopyTo(fileStream);
                    }
                    existingPost.CoverImagePath = "/uploads/posts/" + uniqueFileName;
                }

                _context.Faqs.RemoveRange(existingPost.Faqs);
                if (post.Faqs != null && post.Faqs.Any())
                {
                    foreach (var faq in post.Faqs)
                    {
                        if (!string.IsNullOrWhiteSpace(faq.Question) && !string.IsNullOrWhiteSpace(faq.Answer))
                        {
                            existingPost.Faqs.Add(new Faq { Question = faq.Question, Answer = faq.Answer });
                        }
                    }
                }

                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var post = _context.Posts.Find(id);
            if (post != null)
            {
                if (!string.IsNullOrEmpty(post.CoverImagePath))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.CoverImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Posts.Remove(post);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 🚀 متد جدید: آپلود مستقیم عکس از داخل ویرایشگر متن (CKEditor)
        [HttpPost]
        public IActionResult UploadArticleImage(IFormFile upload)
        {
            if (upload != null && upload.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "article-images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + upload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    upload.CopyTo(fileStream);
                }

                string url = "/uploads/article-images/" + uniqueFileName;

                return Json(new { uploaded = 1, fileName = uniqueFileName, url = url });
            }
            return Json(new { uploaded = 0, error = new { message = "خطا در آپلود عکس." } });
        }
    }
}