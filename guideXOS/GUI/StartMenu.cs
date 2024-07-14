namespace guideXOS.GUI {
    /// <summary>
    /// Start Menu
    /// </summary>
    internal class StartMenu : Window {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        public unsafe StartMenu(int X, int Y, int X2, int Y2) : base(X, Y, X2, Y2) {
            Title = "Start";
        }
    }
}