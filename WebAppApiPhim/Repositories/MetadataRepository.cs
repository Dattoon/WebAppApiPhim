using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppApiPhim.Models;

namespace WebAppApiPhim.Repositories
{
    public class MetadataRepository : IMetadataRepository
    {
        private readonly ApplicationDbContext _context;

        public MetadataRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MovieType>> GetMovieTypesAsync()
        {
            return await _context.MovieTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Genre>> GetGenresAsync()
        {
            return await _context.Genres
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<List<Country>> GetCountriesAsync()
        {
            return await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<string>> GetYearsAsync(int startYear = 2000)
        {
            int currentYear = DateTime.Now.Year;
            var years = new List<string>();

            for (int year = currentYear; year >= startYear; year--)
            {
                years.Add(year.ToString());
            }

            return await Task.FromResult(years);
        }

        public async Task<MovieType> GetMovieTypeByNameAsync(string name)
        {
            return await _context.MovieTypes
                .FirstOrDefaultAsync(t => t.Name == name || t.ApiValue == name);
        }

        public async Task<Genre> GetGenreByNameAsync(string name)
        {
            return await _context.Genres
                .FirstOrDefaultAsync(g => g.Name == name || g.ApiValue == name);
        }

        public async Task<Country> GetCountryByNameAsync(string name)
        {
            return await _context.Countries
                .FirstOrDefaultAsync(c => c.Name == name || c.ApiValue == name);
        }
    }
}