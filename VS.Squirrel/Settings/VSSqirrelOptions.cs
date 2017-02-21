using System.Runtime.Serialization;

namespace AutoSquirrel
{

    /// <summary>
    /// VS Sqirrel Options
    /// </summary>
    /// <seealso cref="AutoSquirrel.IVSSqirrelOptions"/>
    [DataContract]
    public class VSSqirrelOptions : IVSSqirrelOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether [show UI].
        /// </summary>
        /// <value><c>true</c> if [show UI]; otherwise, <c>false</c>.</value>
        [DataMember] public bool ShowUI { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use debug].
        /// </summary>
        /// <value><c>true</c> if [use debug]; otherwise, <c>false</c>.</value>
        [DataMember] public bool UseDebug { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use release].
        /// </summary>
        /// <value><c>true</c> if [use release]; otherwise, <c>false</c>.</value>
        [DataMember] public bool UseRelease { get; set; }
    }
}