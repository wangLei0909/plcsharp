
using Newtonsoft.Json;
using OpenCvSharp;
using Prism.Mvvm;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// 图像数据
    /// </summary>
    public class ImageData : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }

        private string _Name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }


        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
            set { SetProperty(ref _Comment, value); }
        }

        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }

        private Mat _Mat;
        /// <summary>
        /// 图像矩阵
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public Mat Mat
        {
            get { return _Mat; }
            set { SetProperty(ref _Mat, value); }
        }


        /// <summary>
        /// 序列化后的图像数据
        /// </summary>
        [Column("Mat")]
        public byte[] SerializedMat
        {
            get
            {
                byte[] imageBytes = [];
                if (_Mat != null && !_Mat.Empty())
                {

                    Cv2.ImEncode(".png", _Mat, out imageBytes);
                }

                return imageBytes;
            }// 序列化
            set
            {
                try
                {
                    Mat = value != null ? Cv2.ImDecode(value, ImreadModes.Color) : new(); // 自动反序列化
                }
                catch (global::System.Exception)
                {
                    Mat = new();

                }
            }


        }

    }

    public class LocalImageData : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
 
        /// <summary>
        /// 功能标识
        /// </summary>
        public Guid FuncID { get; set; }

        private string _Name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }


        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment
        {
            get { return _Comment; }
            set { SetProperty(ref _Comment, value); }
        }

        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }

        private Mat _Mat;
        /// <summary>
        /// 图像矩阵
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public Mat Mat
        {
            get { return _Mat; }
            set { SetProperty(ref _Mat, value); }
        }


        /// <summary>
        /// 序列化后的图像数据
        /// </summary>
        [Column("Mat")]
        public byte[] SerializedMat
        {
            get
            {
                byte[] imageBytes = [];
                if (_Mat != null && !_Mat.Empty())
                {

                    Cv2.ImEncode(".png", _Mat, out imageBytes);
                }

                return imageBytes;
            }// 序列化
            set
            {
                try
                {
                    Mat = value != null ? Cv2.ImDecode(value, ImreadModes.Color) : new(); // 自动反序列化
                }
                catch (global::System.Exception)
                {
                    Mat = new();

                }
            }


        }

    }
}
