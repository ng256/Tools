using System.Collections.Generic;
using System.Globalization;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace System.ComponentModel
{
    /// <summary>
    ///     An extended URI type converter that can handle various URL formats.
    /// </summary>
    [HostProtection(SecurityAction.LinkDemand, SharedState = true)]
    public class UriConverterExtended : UriTypeConverter
    {
        private static bool _registered;

        /// <summary>
        ///     Register <see cref="UriConverterExtended"/> as default converter for <see cref="Uri"/> class.
        /// </summary>
        public static void Register()
        {
            _registered = _registered || InternalTools.RegisterConverter<Uri, UriConverterExtended>();
        }

        static UriConverterExtended()
        {
            _registered = false;
            Register();
        }

        /// <summary>
        ///     A dictionary that maps well-known port numbers to their corresponding URL schemes.
        /// </summary>
        private static readonly Dictionary<int, string> PortToSchemeMap = new Dictionary<int, string>
        {
            { 80, "http" },
            { 443, "https" },
            { 21, "ftp" },
            { 22, "ssh" },
            { 23, "telnet" },
            { 25, "smtp" },
            { 53, "dns" },
            { 69, "tftp" },
            { 70, "gopher" },
            { 110, "pop3" },
            { 119, "nntp" },
            { 123, "ntp" },
            { 143, "imap" },
            { 389, "ldap" },
            { 465, "smtps" },
            { 587, "smtp" },
            { 808, "net.tcp" },
            { 993, "imaps" },
            { 995, "pop3s" }
        };

        /// <summary>
        ///     Converts the specified object to a Uri instance.
        /// </summary>
        /// <param name="context">
        ///     The type descriptor context.
        /// </param>
        /// <param name="culture">
        ///     The culture information.
        /// </param>
        /// <param name="value">
        ///     The object to convert.
        /// </param>
        /// <returns>
        ///     The converted Uri instance.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Use a regular expression to parse the URL string.
            if (value is string urlString)
            {
                
                Match match = Regex.Match(urlString, "(?:(?<scheme>[a-zA-Z][a-zA-Z0-9+\\-.]*):)?(?:\\/\\/)?" +
                                                     "(?<host>[^\\/\\s:?#]+)?" +
                                                     "(?::(?<port>\\d+))?" +
                                                     "(?<path>\\/[^\\s?#]*)?" +
                                                     "(?:\\?(?<query>[^\\s#]*))?" +
                                                     "(?:#(?<fragment>[^\\s]*))?");
                if (match.Success)
                {
                    // Extract the scheme and port from the URL.
                    string scheme = match.Groups["scheme"].Value;
                    int port = int.TryParse(match.Groups["port"].Value, out int portValue) ? portValue : 0;

                    // If the scheme is empty and the port is known, add the corresponding scheme.
                    if (scheme.IsNullOrEmpty() && port > 0)
                    {
                        urlString = urlString.TrimStart('/');
                        urlString = PortToSchemeMap.TryGetValue(port, out scheme)
                            ? $"{scheme}://{urlString}"
                            : $"http://{urlString}"; // or any other default scheme.
                    }

                    // Create a new Uri instance from the processed URL string.
                    return new Uri(urlString, UriKind.RelativeOrAbsolute);
                }
            }

            // If the conversion fails, fall back to the base class implementation.
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///     Converts the specified Uri object to a string.
        /// </summary>
        /// <param name="context">
        ///     The type descriptor context.
        /// </param>
        /// <param name="culture">
        ///     The culture information.
        /// </param>
        /// <param name="value">
        ///     The object to convert.
        /// </param>
        /// <param name="destinationType">
        ///     The destination type.
        /// </param>
        /// <returns>
        ///     The converted string representation of the Uri.
        /// </returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (value is Uri uri && destinationType == typeof(string))
            {
                return uri.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}