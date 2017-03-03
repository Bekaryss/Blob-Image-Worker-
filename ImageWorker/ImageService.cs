using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ImageWorker
{
    public class ImageService : IImageService
    {
        private readonly string _imageRootPath;
        private readonly string _containerName;
        private readonly string _tableName;
        private readonly string _blobStorageConnectionString;

        public ImageService()
        {
            _imageRootPath = "https://imgstr.blob.core.windows.net/imagesblob";
            _containerName = "imagesblob";
            _tableName = "imagesTable";
            _blobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=imgstr;AccountKey=064YcBNADraf0WRN3q1kGE6ir+gIeoQ8togO2RnILGbMZULOab+IxwKWmRJ2CMXKsnwAAr1t6pOo91AOL46zww==";
        }

        public void ImageCropFactory(string s)
        {
            UploadedImage upImage = GetCurrentImageTable(s);
            AddImageToBlobStorageAsync(upImage);
            ModifyImagesTableContainer(upImage);           
        }

        public void AddImageToBlobStorageAsync(UploadedImage image)
        {
            //  get the container reference
            var container = GetImagesBlobContainer();
            // using the container reference, get a block blob reference and set its type
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("Crop_" + image.Name);
            blockBlob.Properties.ContentType = image.ContentType;
            // finally, upload the image into blob storage using the block blob reference
            var fileBytes = image.CropData;
            blockBlob.UploadFromByteArray(fileBytes, 0, fileBytes.Length);
        }

        public void ModifyImagesTableContainer(UploadedImage image)
        {
            image.Data = null;
            image.CropData = null;

            CloudTable table = GetImagesTable();
            TableOperation updateOperation = TableOperation.Replace(image);
            table.Execute(updateOperation);
        }     

        //Get BlobContainer
        private CloudBlobContainer GetImagesBlobContainer()
        {
            // use the connection string to get the storage account
            var storageAccount = CloudStorageAccount.Parse(_blobStorageConnectionString);
            // using the storage account, create the blob client
            var blobClient = storageAccount.CreateCloudBlobClient();
            // finally, using the blob client, get a reference to our container
            var container = blobClient.GetContainerReference(_containerName);
            // if we had not created the container in the portal, this would automatically create it for us at run time
            container.CreateIfNotExists();
            // by default, blobs are private and would require your access key to download.
            //   You can allow public access to the blobs by making the container public.   
            container.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            return container;
        }

        //Get Tables
        private CloudTable GetImagesTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_blobStorageConnectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(_tableName);
            table.CreateIfNotExists();
            return table;
        }

        public UploadedImage GetCurrentImageTable(string RowId)
        {
            UploadedImage image = new UploadedImage();
            CloudTable table = GetImagesTable();
            TableQuery <UploadedImage> query = new TableQuery<UploadedImage>();         
            image = table.ExecuteQuery(query).Where(p => p.RowKey == RowId).FirstOrDefault();

            var container = GetImagesBlobContainer();         
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(image.Name);

            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                image.Data = memoryStream.ToArray();
            }
            image.URLcrop = string.Format("{0}/{1}", _imageRootPath, "Crop_" + image.Name);
            image.CropData = CreateImageThumbnail(image.Data, 280, 180);
            return image;
        }

        public static byte[] CreateImageThumbnail(byte[] image, int maxWidth, int maxHeight)
        {
            using (var stream = new System.IO.MemoryStream(image))
            {
                var img = Image.FromStream(stream);

                var ratioX = (double)maxWidth / img.Width;
                var ratioY = (double)maxHeight / img.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(img.Width * ratio);
                var newHeight = (int)(img.Height * ratio);

                var thumbnail = img.GetThumbnailImage(newWidth, newHeight, () => false, IntPtr.Zero);

                using (var thumbStream = new System.IO.MemoryStream())
                {
                    thumbnail.Save(thumbStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return thumbStream.GetBuffer();
                }
            }
        }

        
    }
}
