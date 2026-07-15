using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace MaxVerse.API.Helpers;

/// <summary>
/// Helper xử lý tích hợp VNPay Sandbox theo chuẩn của VNPay
/// (https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html)
/// </summary>
public class VnPayHelper
{
    private readonly IConfiguration _config;

    public VnPayHelper(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay để redirect người dùng tới
    /// </summary>
    public string CreatePaymentUrl(string orderCode, decimal amount, string orderInfo, string ipAddress)
    {
        var vnpSettings = _config.GetSection("VnPaySettings");
        var tmnCode = vnpSettings["TmnCode"]!;
        var hashSecret = vnpSettings["HashSecret"]!;
        var baseUrl = vnpSettings["BaseUrl"]!;
        var returnUrl = vnpSettings["ReturnUrl"]!;

        var vnpParams = new SortedList<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", tmnCode },
            // VNPay yêu cầu đơn vị là VND x 100 (không có phần thập phân)
            { "vnp_Amount", ((long)(amount * 100)).ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", orderCode },
            { "vnp_OrderInfo", orderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_IpAddr", ipAddress },
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss") }
        };

        var query = BuildQueryString(vnpParams);
        var signData = BuildSignData(vnpParams);
        var secureHash = HmacSha512(hashSecret, signData);

        return $"{baseUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    /// <summary>
    /// Validate chữ ký trả về từ VNPay (gọi khi xử lý ReturnUrl / IPN)
    /// </summary>
    public bool ValidateSignature(IQueryCollection queryParams)
    {
        var vnpSettings = _config.GetSection("VnPaySettings");
        var hashSecret = vnpSettings["HashSecret"]!;

        var receivedHash = queryParams["vnp_SecureHash"].ToString();
        if (string.IsNullOrEmpty(receivedHash)) return false;

        var sorted = new SortedList<string, string>(StringComparer.Ordinal);
        foreach (var pair in queryParams)
        {
            if (pair.Key == "vnp_SecureHash" || pair.Key == "vnp_SecureHashType") continue;
            sorted.Add(pair.Key, pair.Value.ToString());
        }

        var signData = BuildSignData(sorted);
        var computedHash = HmacSha512(hashSecret, signData);

        return computedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildQueryString(SortedList<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var kv in data)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append(HttpUtility.UrlEncode(kv.Key)).Append('=').Append(HttpUtility.UrlEncode(kv.Value));
        }
        return sb.ToString();
    }

    private static string BuildSignData(IDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var kv in data)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            if (sb.Length > 0) sb.Append('&');
            sb.Append(HttpUtility.UrlEncode(kv.Key)).Append('=').Append(HttpUtility.UrlEncode(kv.Value));
        }
        return sb.ToString();
    }

    private static string HmacSha512(string key, string input)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
