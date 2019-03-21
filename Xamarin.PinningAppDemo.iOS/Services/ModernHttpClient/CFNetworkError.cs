//
// Copyright(c) 2013 Paul Betts
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using Foundation;

namespace Xamarin.PinningAppDemo.iOS.Services.ModernHttpClient
{
    public static class CFNetworkError
    {
        public static NSString ErrorDomain { get { return new NSString("kCFErrorDomainCFNetwork"); } }
    }

    // From Apple reference docs:
    // https://developer.apple.com/library/ios/documentation/Networking/Reference/CFNetworkErrors/#//apple_ref/c/tdef/CFNetworkErrors
    public enum CFNetworkErrors
    {
        CFHostErrorHostNotFound = 1,
        CFHostErrorUnknown = 2,

        // SOCKS errors
        CFSOCKSErrorUnknownClientVersion = 100,
        CFSOCKSErrorUnsupportedServerVersion = 101,

        // SOCKS4-specific errors
        CFSOCKS4ErrorRequestFailed = 110,
        CFSOCKS4ErrorIdentdFailed = 111,
        CFSOCKS4ErrorIdConflict = 112,
        CFSOCKS4ErrorUnknownStatusCode = 113,

        // SOCKS5-specific errors
        CFSOCKS5ErrorBadState = 120,
        CFSOCKS5ErrorBadResponseAddr = 121,
        CFSOCKS5ErrorBadCredentials = 122,
        CFSOCKS5ErrorUnsupportedNegotiationMethod = 123,
        CFSOCKS5ErrorNoAcceptableMethod = 124,

        // FTP errors
        CFFTPErrorUnexpectedStatusCode = 200,

        // HTTP errors
        CFErrorHttpAuthenticationTypeUnsupported = 300,
        CFErrorHttpBadCredentials = 301,
        CFErrorHttpConnectionLost = 302,
        CFErrorHttpParseFailure = 303,
        CFErrorHttpRedirectionLoopDetected = 304,
        CFErrorHttpBadURL = 305,
        CFErrorHttpProxyConnectionFailure = 306,
        CFErrorHttpBadProxyCredentials = 307,
        CFErrorPACFileError = 308,
        CFErrorPACFileAuth = 309,
        CFErrorHttpsProxyConnectionFailure = 310,
        CFStreamErrorHttpsProxyFailureUnexpectedResponseToConnectMethod = 311,

        // CFURL and CFURLConnection Errors
        CFURLErrorUnknown = -998,
        CFURLErrorCancelled = -999,
        CFURLErrorBadURL = -1000,
        CFURLErrorTimedOut = -1001,
        CFURLErrorUnsupportedURL = -1002,
        CFURLErrorCannotFindHost = -1003,
        CFURLErrorCannotConnectToHost = -1004,
        CFURLErrorNetworkConnectionLost = -1005,
        CFURLErrorDNSLookupFailed = -1006,
        CFURLErrorHTTPTooManyRedirects = -1007,
        CFURLErrorResourceUnavailable = -1008,
        CFURLErrorNotConnectedToInternet = -1009,
        CFURLErrorRedirectToNonExistentLocation = -1010,
        CFURLErrorBadServerResponse = -1011,
        CFURLErrorUserCancelledAuthentication = -1012,
        CFURLErrorUserAuthenticationRequired = -1013,
        CFURLErrorZeroByteResource = -1014,
        CFURLErrorCannotDecodeRawData = -1015,
        CFURLErrorCannotDecodeContentData = -1016,
        CFURLErrorCannotParseResponse = -1017,
        CFURLErrorInternationalRoamingOff = -1018,
        CFURLErrorCallIsActive = -1019,
        CFURLErrorDataNotAllowed = -1020,
        CFURLErrorRequestBodyStreamExhausted = -1021,
        CFURLErrorFileDoesNotExist = -1100,
        CFURLErrorFileIsDirectory = -1101,
        CFURLErrorNoPermissionsToReadFile = -1102,
        CFURLErrorDataLengthExceedsMaximum = -1103,

        // SSL errors
        CFURLErrorSecureConnectionFailed = -1200,
        CFURLErrorServerCertificateHasBadDate = -1201,
        CFURLErrorServerCertificateUntrusted = -1202,
        CFURLErrorServerCertificateHasUnknownRoot = -1203,
        CFURLErrorServerCertificateNotYetValid = -1204,
        CFURLErrorClientCertificateRejected = -1205,
        CFURLErrorClientCertificateRequired = -1206,

        CFURLErrorCannotLoadFromNetwork = -2000,

        // Download and file I/O errors
        CFURLErrorCannotCreateFile = -3000,
        CFURLErrorCannotOpenFile = -3001,
        CFURLErrorCannotCloseFile = -3002,
        CFURLErrorCannotWriteToFile = -3003,
        CFURLErrorCannotRemoveFile = -3004,
        CFURLErrorCannotMoveFile = -3005,
        CFURLErrorDownloadDecodingFailedMidStream = -3006,
        CFURLErrorDownloadDecodingFailedToComplete = -3007,

        // Cookie errors
        CFHTTPCookieCannotParseCookieFile = -4000,

        // Errors originating from CFNetServices
        CFNetServiceErrorUnknown = -72000,
        CFNetServiceErrorCollision = -72001,
        CFNetServiceErrorNotFound = -72002,
        CFNetServiceErrorInProgress = -72003,
        CFNetServiceErrorBadArgument = -72004,
        CFNetServiceErrorCancel = -72005,
        CFNetServiceErrorInvalid = -72006,
        CFNetServiceErrorTimeout = -72007,
        CFNetServiceErrorDNSServiceFailure = -73000,
    }
}
