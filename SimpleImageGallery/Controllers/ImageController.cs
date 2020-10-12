using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleImageGallery.Data;
using SimpleImageGallery.Models;
using SimpleImageGallery.Services;

namespace SimpleImageGallery.Controllers
{
    public class ImageController : Controller
    {
        private IConfiguration _config;
        private string AzureStorageConnectionString { get; }

        private readonly IImage _imageService;

        public ImageController(IConfiguration config, IImage imageService)
        {
            _imageService = imageService;
            _config = config;
            AzureStorageConnectionString = _config["AzureStorageConnectionString"];
        }
        public IActionResult Upload()
        {
            var model = new UploadImageModel();
            //await DownloadImageToDisplay("interiordesk.jpg");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadNewImage(IFormFile file, string title, string tags)
        {
            var container = _imageService.GetBlobContainer(AzureStorageConnectionString, "images");
            var content = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
            var fileName = content.FileName.Trim('"');
            var blockBlob = container.GetBlockBlobReference(fileName);

            await blockBlob.UploadFromStreamAsync(file.OpenReadStream());
            await _imageService.SetImage(title, tags, blockBlob.Uri);

            return RedirectToAction("Index", "Gallery");
        }

        public async Task<IActionResult> DownloadImageToDisplay()
        {
            string filename = "interiordesk.jpg";
            var container = _imageService.GetBlobContainer(AzureStorageConnectionString, "images");
            if (await container.ExistsAsync())
            {
                var fileNm = filename.Trim('"');
                var blobref = container.GetBlobReference(fileNm);
                if (await blobref.ExistsAsync())
                {
                    MemoryStream ms = new MemoryStream();
                    await blobref.DownloadToStreamAsync(ms);
                    Stream blobStream = blobref.OpenReadAsync().Result;
                    byte[] bytes;
                    using (var memStream = new MemoryStream())
                    {
                        blobStream.CopyTo(memStream);
                        bytes = memStream.ToArray();
                    }
                    string base64 = Convert.ToBase64String(bytes);
                    ViewBag.base64 = "data: image / png; base64," + base64;
                    return View();
                }
                else
                {
                    return Content("File does not exist");
                }
            }
            else
            {
                return Content("Container does not exist");
            }
        }
    }
}