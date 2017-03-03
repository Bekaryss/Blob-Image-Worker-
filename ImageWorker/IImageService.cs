using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageWorker
{
    public interface IImageService
    {
        void AddImageToBlobStorageAsync(UploadedImage image);
        void ModifyImagesTableContainer(UploadedImage image);
        UploadedImage GetCurrentImageTable(string RowId);

        void ImageCropFactory(string s);
        
    }
}
