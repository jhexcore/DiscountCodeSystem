using DiscountServer.Models;
using System.Text.Json;

namespace DiscountServer
{
    public class DiscountManager
    {
        private readonly string _storageFile;
        private readonly object _lock = new();
        private readonly HashSet<string> _existingCodes;
        private List<DiscountCode> _codes;

        public DiscountManager(string? storagePath = null)
        {
            _storageFile = storagePath ?? Path.Combine(AppContext.BaseDirectory, "codes.json");
            if (File.Exists(_storageFile))
            {
                var json = File.ReadAllText(_storageFile);
                _codes = JsonSerializer.Deserialize<List<DiscountCode>>(json) ?? new List<DiscountCode>();
            }
            else
            {
                _codes = new List<DiscountCode>();
            }

            _existingCodes = new HashSet<string>(_codes.Select(c => c.Code));
        }

        private void Save()
        {
            var tmp = _storageFile + ".tmp";
            var json = JsonSerializer.Serialize(_codes, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmp, json);
            File.Move(tmp, _storageFile, overwrite: true);
        }

        public List<string> GenerateCodes(int count, int? length = null)
        {
            if (count <= 0 || count > 2000)
                throw new ArgumentException("Count must be between 1 and 2000");

            if (length is not null && length is not (7 or 8))
                throw new ArgumentException("Length must be either 7 or 8");

            var random = Random.Shared;
            var newCodes = new List<string>(capacity: count);

            lock (_lock)
            {
                while (newCodes.Count < count)
                {
                    int len = (length is not null && length is 7 or 8) ? (int)length : random.Next(7, 9);
                    var code = GenerateRandomCode(len, random);

                    if (_existingCodes.Add(code))
                    {
                        _codes.Add(new DiscountCode { Code = code });
                        newCodes.Add(code);
                    }
                }
                Save();
            }
            return newCodes;
        }

        public string UseCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "ERROR: Code is required";

            lock (_lock)
            {
                var discount = _codes.FirstOrDefault(c => c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));
                if (discount == null) return "ERROR: Code does not exist";
                if (discount.IsUsed) return "ERROR: Code already used";

                discount.IsUsed = true;
                Save();
                return $"SUCCESS: Code {discount.Code} used";
            }
        }

        private static string GenerateRandomCode(int length, Random random)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> buffer = stackalloc char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[random.Next(chars.Length)];
            }
            return new string(buffer);
        }
    }
}
