// dotnet dev-certs https --trust
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Threading.Tasks;
using Azure;

namespace AzureFileUpload.Pages
{
    [Authorize]
    public class UploadModel : PageModel
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public IList<IFormFile> Files { get; set; } = new List<IFormFile>();

        [BindProperty]
        public bool Overwrite { get; set; }

        public UploadModel(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Files.Count == 0)
            {
                ModelState.AddModelError("Files", "Please select at least one file to upload.");
                return Page();
            }

            var containerName = _configuration["StorageAccount:Container"];
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var successfulUploads = new List<string>();
            var failedUploads = new List<string>();

            foreach (var file in Files)
            {
                bool skip = false;
                var blobClient = containerClient.GetBlobClient(file.FileName);

                try
                {
                    // Check if blob already exists
                    var blobExists = await blobClient.ExistsAsync();

                    if (blobExists.Value)
                    {
                        if (!Overwrite)
                        {
                            failedUploads.Add(file.FileName);
                            skip = true;
                        }
                        else
                        {
                            // Overwrite allowed, delete the existing blob
                            await blobClient.DeleteIfExistsAsync();
                        }
                    }
                }
                catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.AuthorizationPermissionMismatch)
                {
                    TempData["Failure"] = "You do not have permission to upload files. Please contact the administrator.";
                    return Page();
                }

                if (skip)
                {
                    continue;
                }

                try
                {
                    using (var stream = file.OpenReadStream())
                    {
                        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } });
                    }

                    successfulUploads.Add(file.FileName);
                }
                catch (RequestFailedException ex)
                {
                    if (ex.ErrorCode == BlobErrorCode.AuthorizationPermissionMismatch)
                    {
                        TempData["Failure"] = "You do not have permission to upload files. Please contact the administrator.";
                        return Page();
                    }

                    failedUploads.Add(file.FileName);
                }
            }

            if (successfulUploads.Any())
            {
                TempData["Success"] = $"Successfully uploaded files:<br/>" + string.Join("<br/>", successfulUploads);
            }

            if (failedUploads.Any())
            {
                TempData["Failure"] += "Failed to upload files because they already exist:<br/>" + string.Join("<br/>", failedUploads);
            }

            return RedirectToPage();
        }
   }
}