namespace AutoSquirrel
{
    /// <summary>
    /// Interaction logic for OptionsPage.xaml
    /// </summary>
    public partial class OptionsControl : IVSSqirrelOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsControl"/> class.
        /// </summary>
        public OptionsControl() => InitializeComponent();

        /// <summary>
        /// Gets or sets a value indicating whether [show UI].
        /// </summary>
        /// <value><c>true</c> if [show UI]; otherwise, <c>false</c>.</value>
        public bool ShowUI
        {
            get => this.showUI.IsChecked ?? false; set => this.showUI.IsChecked = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use debug].
        /// </summary>
        /// <value><c>true</c> if [use debug]; otherwise, <c>false</c>.</value>
        public bool UseDebug
        {
            get => this.useDebug.IsChecked ?? false; set => this.useDebug.IsChecked = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [use release].
        /// </summary>
        /// <value><c>true</c> if [use release]; otherwise, <c>false</c>.</value>
        public bool UseRelease
        {
            get => this.useRelease.IsChecked ?? false; set => this.useRelease.IsChecked = value;
        }
    }
}