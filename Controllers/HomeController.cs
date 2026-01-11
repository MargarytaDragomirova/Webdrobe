using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public HomeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Home
        public async Task<IActionResult> Index()
        {
            var clothes = await _context.Clothes.ToListAsync();
            return View(clothes);
        }

        // GET: /Home/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Home/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cloth cloth, List<IFormFile>? imageFiles)
        {
            if (ModelState.IsValid)
            {
                var imagePaths = new List<string>();

                if (imageFiles != null && imageFiles.Any())
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "clothes");
                    Directory.CreateDirectory(uploadsFolder);

                    foreach (var imageFile in imageFiles.Take(10)) // limit to 10 images
                    {
                        if (imageFile.Length > 0)
                        {
                            string uniqueFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(fileStream);
                            }

                            imagePaths.Add($"/images/clothes/{uniqueFileName}");
                        }
                    }
                }

                cloth.ImagePaths = imagePaths;
                _context.Add(cloth);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(cloth);
        }

        // GET: /Home/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth == null) return NotFound();
            return View(cloth);
        }

        // GET: /Home/GetClothes
        public async Task<List<Cloth>> GetClothes()
        {
            var clothes = await _context.Clothes.ToListAsync();
            return clothes;
        }

        [HttpGet("GetCloth/{id}")]
        public async Task<IActionResult> GetCloth(int id)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth == null)
                return NotFound(new { message = "Cloth not found" });

            return Ok(cloth);
        }

        [HttpPut("UpdateCloth/{id}")]
        public async Task<IActionResult> UpdateCloth(int id, [FromForm] Cloth clothUpdate, List<IFormFile>? imageFiles)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth == null)
                return NotFound("Cloth not found");

            cloth.Name = clothUpdate.Name;
            cloth.Category = clothUpdate.Category;
            cloth.Color = clothUpdate.Color;
            cloth.Season = clothUpdate.Season;
            cloth.Location = clothUpdate.Location;

            // handle new images
            if (imageFiles != null && imageFiles.Any())
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "clothes");
                Directory.CreateDirectory(uploadsFolder);

                var newPaths = new List<string>();

                foreach (var file in imageFiles.Take(10))
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    newPaths.Add($"/images/clothes/{fileName}");
                }

                cloth.ImagePaths = newPaths;
            }

            _context.Update(cloth);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, cloth });
        }


        [HttpDelete]
        public async Task<string> DeleteCloth(int id)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth != null)
            {
                _context.Clothes.Remove(cloth);
                await _context.SaveChangesAsync();
                return "Cloth " + cloth.Name + " deleted";
            }
            return "Cloth not found";
        }



        [HttpGet("GetOutfits")]
        public async Task<List<Outfit>> GetOutfitsApi()
        {
            var outfits = await _context.Outfits.ToListAsync();
            foreach (var item in outfits)
            {
                foreach (var clothId in item.ClothIds)
                {
                    var cloth = await _context.Clothes.FirstOrDefaultAsync(x=> x.Id == clothId);
                    if (cloth != null)
                    {
                        item.Cloths ??= [];
                        item.Cloths.Add(cloth);
                    }
                }
                _ = _context.Outfits.Update(item);
            }
            return outfits;
        }

        [HttpGet("GetOutfit/{id}")]
        public async Task<IActionResult> GetOutfitApi(int id)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit == null)
                return NotFound();

            var clothes = await _context.Clothes
                .Where(c => outfit.ClothIds.Contains(c.Id))
                .ToListAsync();

            return Ok(new
            {
                outfit.Id,
                outfit.Name,
                Clothes = clothes
            });
        }

        [HttpPost("CreateOutfitApi")]
        public async Task<IActionResult> CreateOutfitApi([FromBody] Outfit outfit)
        {
            if (string.IsNullOrWhiteSpace(outfit.Name) || outfit.ClothIds.Count < 2)
                return BadRequest("Outfit must contain at least 2 items.");

            _context.Outfits.Add(outfit);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, outfit });
        }

        [HttpPut("UpdateOutfit/{id}")]
        public async Task<IActionResult> UpdateOutfit(int id, [FromBody] Outfit update)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit == null)
                return NotFound();

            outfit.Name = update.Name;
            outfit.ClothIds = update.ClothIds;

            _context.Update(outfit);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, outfit });
        }

        [HttpDelete("DeleteOutfitApi/{id}")]
        public async Task<IActionResult> DeleteOutfitApi(int id)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit == null)
                return NotFound();

            _context.Outfits.Remove(outfit);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("FilterClothes")]
        public async Task<IActionResult> FilterClothes(string? category, string? color, string? season, string? location)
        {
            var query = _context.Clothes.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(c => c.Category == category);

            if (!string.IsNullOrEmpty(color))
                query = query.Where(c => c.Color == color);

            if (!string.IsNullOrEmpty(season))
                query = query.Where(c => c.Season == season);

            if (!string.IsNullOrEmpty(location))
                query = query.Where(c => c.Location == location);

            return Ok(await query.ToListAsync());
        }









      [HttpPost("CreateCloth")]
        public async Task<IActionResult> CreateCloth([FromForm] Cloth cloth, List<IFormFile>? imageFiles)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var imagePaths = new List<string>();

            if (imageFiles != null && imageFiles.Any())
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "clothes");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var imageFile in imageFiles.Take(10)) // limit to 10 images
                {
                    if (imageFile.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        imagePaths.Add($"/images/clothes/{uniqueFileName}");
                    }
                }
            }

            cloth.ImagePaths = imagePaths;

            _context.Clothes.Add(cloth);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Cloth '{cloth.Name}' created successfully",
                cloth
            });
        }

        

        // POST: /Home/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Cloth updatedCloth, List<IFormFile>? imageFiles)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth == null) return NotFound();

            cloth.Name = updatedCloth.Name;
            cloth.Category = updatedCloth.Category;
            cloth.Color = updatedCloth.Color;
            cloth.Season = updatedCloth.Season;
            cloth.Location = updatedCloth.Location;

            if (imageFiles != null && imageFiles.Count > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "clothes");
                Directory.CreateDirectory(uploadsFolder);

                var newPaths = new List<string>();

                foreach (var file in imageFiles.Take(10))
                {
                    if (file.Length > 0)
                    {
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        newPaths.Add($"/images/clothes/{fileName}");
                    }
                }

                // Replace old images with new ones
                cloth.ImagePaths = newPaths;
            }

            _context.Update(cloth);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Home/Details/{id}
        public IActionResult Details(int id)
        {
            var cloth = _context.Clothes.FirstOrDefault(c => c.Id == id);
            if (cloth == null)
            {
                return NotFound();
            }

            return View(cloth);
        }

        public async Task<IActionResult> Categories(string? selectedCategory)
        {
            var allClothes = await _context.Clothes.ToListAsync();

            if (!string.IsNullOrEmpty(selectedCategory))
            {
                allClothes = allClothes
                    .Where(c => c.Category == selectedCategory)
                    .ToList();
            }

            ViewBag.SelectedCategory = selectedCategory;
            ViewBag.AllClothes = allClothes;

            return View();
        }

        // ========================= OUTFITS =========================

        // GET: /Home/Outfits
        // GET: /Home/CreateOutfit
        
        public async Task<IActionResult> CreateOutfit()
        {
            var clothes = await _context.Clothes.ToListAsync();
            return View(clothes); // pass clothes to view
        }

        // POST: /Home/CreateOutfit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOutfit(string name, List<int> selectedClothIds)
        {
            if (string.IsNullOrWhiteSpace(name) || selectedClothIds == null || selectedClothIds.Count < 2)
            {
                ModelState.AddModelError("", "An outfit must contain at least 2 items.");
                var clothes = await _context.Clothes.ToListAsync();
                return View(clothes);
            }

            var outfit = new Outfit
            {
                Name = name,
                ClothIds = selectedClothIds
            };

            _context.Outfits.Add(outfit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Outfits));
        }


        // GET: /Home/Outfits
        public async Task<IActionResult> Outfits()
        {
            var outfits = await _context.Outfits.ToListAsync();
            var clothes = await _context.Clothes.ToListAsync();

            // Join outfits with their clothes
            var outfitViewModels = outfits.Select(o => new
            {
                Outfit = o,
                Clothes = clothes.Where(c => o.ClothIds.Contains(c.Id)).ToList()
            }).ToList();

            return View(outfitViewModels);
        }

        // GET: /Home/OutfitDetails/{id}
        public async Task<IActionResult> OutfitDetails(int id)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit == null)
                return NotFound();

            var clothes = await _context.Clothes
                .Where(c => outfit.ClothIds.Contains(c.Id))
                .ToListAsync();

            ViewBag.OutfitName = outfit.Name;
            return View(clothes);
        }
        // GET: /Home/EditOutfit/{id}
        public async Task<IActionResult> EditOutfit(int id)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit == null) return NotFound();

            var clothes = await _context.Clothes.ToListAsync();

            ViewBag.Clothes = clothes;
            return View(outfit);
        }

        // POST: /Home/EditOutfit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOutfit(int id, Outfit updatedOutfit, List<int> selectedClothIds)
        {
            if (id != updatedOutfit.Id)
                return NotFound();

            if (selectedClothIds == null || selectedClothIds.Count < 2)
            {
                ModelState.AddModelError("", "An outfit must contain at least 2 items.");
                var clothes = await _context.Clothes.ToListAsync();
                ViewBag.Clothes = clothes;
                return View(clothes);
            }


            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit == null) return NotFound();

            outfit.Name = updatedOutfit.Name;
            outfit.ClothIds = selectedClothIds;

            _context.Update(outfit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Outfits));
        }


        // POST: /Home/DeleteOutfit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOutfit(int id)
        {
            var outfit = await _context.Outfits.FindAsync(id);
            if (outfit != null)
            {
                _context.Outfits.Remove(outfit);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Outfits));
        }



        // GET: /Home/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth == null) return NotFound();
            return View(cloth);
        }

        // POST: /Home/DeleteConfirmed/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cloth = await _context.Clothes.FindAsync(id);
            if (cloth != null)
            {
                _context.Clothes.Remove(cloth);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX Search
        public async Task<IActionResult> Search(string query)
        {
            var clothes = string.IsNullOrWhiteSpace(query)
                ? await _context.Clothes.ToListAsync()
                : await _context.Clothes
                    .Where(c => c.Name.Contains(query) || c.Category.Contains(query) || c.Color.Contains(query))
                    .ToListAsync();

            return PartialView("_ClothesListPartial", clothes);
        }
    }
}
