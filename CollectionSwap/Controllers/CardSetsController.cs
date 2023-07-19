using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using CollectionSwap.Models;

namespace CollectionSwap.Controllers
{
    public class CardSetsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Edit(int? id)
        {
            CardSet cardSet = db.CardSets.Find(id);

            string path = Server.MapPath("~/Card_Sets/" + cardSet.card_set_id);
            string[] files = Directory.GetFiles(path);
            files = files.Select(fileName => Path.GetFileName(fileName)).ToArray();

            ViewBag.Cards = files.OrderBy(f => f.Length);
            ViewBag.Status = TempData["Success"];
            return View(cardSet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CardSet cardSet) 
        {
            if (ModelState.IsValid)
            {
                db.Entry(cardSet).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Card set name updated successfully.";
                return RedirectToAction("Edit");
            }
            return View(cardSet);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateCardSet cardSet)
        {
            if (ModelState.IsValid)
            {
                if (cardSet.FileInput != null && cardSet.FileInput.ContentLength > 0)
                {
                    CardSet newCardSet = new CardSet()
                    {
                        card_set_name = cardSet.card_set_name
                    };
                    db.CardSets.Add(newCardSet);
                    db.SaveChanges();

                    // Get the file name and file extension
                    string fileName = Path.GetFileName(cardSet.FileInput.FileName);
                    string fileExtension = Path.GetExtension(fileName);

                    // Generate a unique file name to prevent overwriting files with the same name
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;

                    // Specify the path where you want to save the uploaded file on the server
                    string uploadPath = Server.MapPath("~/Card_Sets/" + newCardSet.card_set_id); // Update the path as needed

                    // Ensure the target directory exists; create it if it doesn't
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Save the file to the server
                    string filePath = Path.Combine(uploadPath, uniqueFileName);
                    cardSet.FileInput.SaveAs(filePath);

                    // Optionally, you can save the file name or file path to your database
                    // Depending on your application's requirements.

                    // ... Continue with your logic for saving the card set ...

                    return RedirectToAction("Index", "Manage");
                }
            }
            return View(cardSet);
        }
    }
}