using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Lab3JsonMaui.Models;

namespace Lab3JsonMaui.Services
{
    // серіалізація / десеріалізація JSON
    public class JsonStorageService
    {
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public async Task<List<ParliamentEvent>> LoadFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return new List<ParliamentEvent>();

            await using var stream = File.OpenRead(filePath);
            var items = await JsonSerializer.DeserializeAsync<List<ParliamentEvent>>(stream, _options);
            return items ?? new List<ParliamentEvent>();
        }

        public async Task SaveToFileAsync(string filePath, IEnumerable<ParliamentEvent> items)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, items, _options);
        }
    }
}