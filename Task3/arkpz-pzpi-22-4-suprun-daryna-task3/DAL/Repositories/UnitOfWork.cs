using Core.Models;
using Microsoft.Extensions.Logging;

namespace DAL.Repositories
{
    public class UnitOfWork : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ApplicationContext _context;
        private bool disposed = false;

        private readonly Dictionary<Type, object> _repositories = new();

        private Repository<Device> _deviceRepository;
        private Repository<Elixir> _elixirRepository;
        private Repository<ElixirComposition> _elixirCompositionRepository;
        private Repository<Note> _noteRepository;
        private Repository<History> _historyRepository;
        private Repository<Preferences> _preferencesRepository;

        public Repository<Device> DeviceRepository
        {
            get
            {
                _deviceRepository ??= new Repository<Device>(_context,
                    new Logger<Repository<Device>>(_loggerFactory));

                return _deviceRepository;
            }
        }

        public Repository<Elixir> ElixirRepository
        {
            get
            {
                _elixirRepository ??= new Repository<Elixir>(_context,
                    new Logger<Repository<Elixir>>(_loggerFactory));

                return _elixirRepository;
            }
        }

        public Repository<ElixirComposition> ElixirCompositionRepository
        {
            get
            {
                _elixirCompositionRepository ??= new Repository<ElixirComposition>(_context,
                    new Logger<Repository<ElixirComposition>>(_loggerFactory));

                return _elixirCompositionRepository;
            }
        }

        public Repository<Note> NoteRepository
        {
            get
            {
                _noteRepository ??= new Repository<Note>(_context,
                    new Logger<Repository<Note>>(_loggerFactory));

                return _noteRepository;
            }
        }

        public Repository<History> HistoryRepository
        {
            get
            {
                _historyRepository ??= new Repository<History>(_context,
                    new Logger<Repository<History>>(_loggerFactory));

                return _historyRepository;
            }
        }
        public Repository<Preferences> PreferencesRepository
        {
            get
            {
                _preferencesRepository ??= new Repository<Preferences>(_context,
                    new Logger<Repository<Preferences>>(_loggerFactory));

                return _preferencesRepository;
            }
        }

        public UnitOfWork(ApplicationContext context, ILoggerFactory loggerFactory)
        {
            _context = context;
            _loggerFactory = loggerFactory;
            _logger = new Logger<UnitOfWork>(_loggerFactory);
        }
        public IRepository<T> GetRepository<T>() where T : class
        {
            if (!_repositories.ContainsKey(typeof(T)))
            {
                var repositoryInstance = new Repository<T>(_context, new Logger<Repository<T>>(_loggerFactory));
                _repositories.Add(typeof(T), repositoryInstance);
            }

            return (IRepository<T>)_repositories[typeof(T)];
        }

        public async Task<int> Save()
        {
            try
            {
                _logger.LogInformation("Saving changes to the database");

                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save changes to the database! Error: {errorMessage}", ex.Message);

                throw new Exception($"Failed to save changes to the database: {ex.Message}");
            }
        }

        protected virtual async Task Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    await _context.DisposeAsync();
                }
            }

            disposed = true;
        }

        public async void Dispose()
        {
            await Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
