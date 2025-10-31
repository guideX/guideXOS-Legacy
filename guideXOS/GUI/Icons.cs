using guideXOS.FS;
using guideXOS.Misc;
using System.Drawing;
namespace guideXOS.GUI {
    /// <summary>
    /// Icons
    /// </summary>
    public static class Icons {
        private static Image _fileIcon;
        private static Image _imageIcon;
        private static Image _audioIcon;
        private static Image _folderIcon;
        private static Image _taskbarIcon;
        private static Image _startIcon;

        /// <summary>
        /// File Icon
        /// </summary>
        public static Image FileIcon {
            get {
                if (_fileIcon == null) _fileIcon = new PNG(File.ReadAllBytes("Images/file.png"));
                return _fileIcon;
            }
        }
        /// <summary>
        /// Iamge Icon
        /// </summary>
        public static Image IamgeIcon {
            get {
                if (_imageIcon == null) _imageIcon = new PNG(File.ReadAllBytes("Images/Image.png"));
                return _imageIcon;
            }
        }
        /// <summary>
        /// Audio Icon
        /// </summary>
        public static Image AudioIcon {
            get {
                if (_audioIcon == null) _audioIcon = new PNG(File.ReadAllBytes("Images/Audio.png"));
                return _audioIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image FolderIcon {
            get {
                if (_folderIcon == null) _folderIcon = new PNG(File.ReadAllBytes("Images/Folder.png"));
                return _folderIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image TaskbarIcon {
            get {
                if (_taskbarIcon == null) _taskbarIcon = new PNG(File.ReadAllBytes("Images/taskbar.png"));
                return _taskbarIcon;
            }
        }
        /// <summary>
        /// Folder Icon
        /// </summary>
        public static Image StartIcon {
            get {
                if (_startIcon == null) _startIcon = new PNG(File.ReadAllBytes("Images/Start.png"));
                return _startIcon;
            }
        }
    }
}