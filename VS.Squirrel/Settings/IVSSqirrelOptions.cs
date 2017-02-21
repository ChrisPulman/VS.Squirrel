namespace AutoSquirrel
{
    /// <summary>
    /// interface for VS Sqirrel Options
    /// </summary>
    public interface IVSSqirrelOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether [show UI].
        /// </summary>
        /// <value><c>true</c> if [show UI]; otherwise, <c>false</c>.</value>
        bool ShowUI { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use debug].
        /// </summary>
        /// <value><c>true</c> if [use debug]; otherwise, <c>false</c>.</value>
        bool UseDebug { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use release].
        /// </summary>
        /// <value><c>true</c> if [use release]; otherwise, <c>false</c>.</value>
        bool UseRelease { get; set; }
    }
}