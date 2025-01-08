using BLL.Interfaces;
using Core.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class NoteService : GenericService<Note>, INoteService
    {
        private readonly IRepository<Note> _noteRepository;
        private readonly IRepository<Elixir> _elixirRepository;
        private readonly ILogger<NoteService> _logger;

        public NoteService(UnitOfWork unitOfWork, ILogger<NoteService> logger)
            : base(unitOfWork, unitOfWork.NoteRepository)
        {
            _noteRepository = unitOfWork.NoteRepository;
            _elixirRepository = unitOfWork.ElixirRepository;
            _logger = logger;
        }

        public async Task<Result<List<Note>>> GetAllNotes()
        {
            _logger.LogInformation("Fetching all available notes.");

            try
            {
                var notes = await _noteRepository.GetAsync();

                if (notes == null || !notes.Any())
                {
                    _logger.LogWarning("No notes found in the database.");
                    return new Result<List<Note>>(false);
                }

                _logger.LogInformation("Successfully retrieved {Count} notes.", notes.Count);
                return new Result<List<Note>>(true, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while fetching notes: {Message}", ex.Message);
                return new Result<List<Note>>(false);
            }
        }

        public async Task<Result> AddNotesToElixir(int elixirId, List<(int noteId, string noteCategory, decimal proportion)> notesData)
        {
            _logger.LogInformation("Adding notes to elixir with ID {ElixirId}.", elixirId);

            try
            {
                var elixir = await _elixirRepository.GetByIdAsync(elixirId);
                if (elixir == null)
                {
                    _logger.LogWarning("Elixir with ID {ElixirId} not found.", elixirId);
                    return new Result(false);
                }

                foreach (var (noteId, noteCategory, proportion) in notesData)
                {
                    var note = await _noteRepository.GetByIdAsync(noteId);
                    if (note == null)
                    {
                        _logger.LogWarning("Note with ID {NoteId} not found. Skipping.", noteId);
                        continue;
                    }

                    var elixirComposition = new ElixirComposition
                    {
                        ElixirId = elixirId,
                        NoteId = noteId,
                        NoteCategory = noteCategory,
                        Proportion = proportion
                    };

                    elixir.ElixirComposition.Add(elixirComposition);
                }

                await _unitOfWork.Save();

                _logger.LogInformation("Notes successfully added to elixir with ID {ElixirId}.", elixirId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding notes to elixir with ID {ElixirId}. Error: {Error}", elixirId, ex.Message);
                return new Result(false);
            }
        }


        public async Task<Result<Note>> CreateNote(Note note)
        {
            _logger.LogInformation("Attempting to add a new note.");

            if (note == null || string.IsNullOrWhiteSpace(note.Name))
            {
                _logger.LogWarning("Failed to add note - invalid data provided.");
                return new Result<Note>(false);
            }

            try
            {
                var existingNote = await _noteRepository.GetAsync(n => n.Name == note.Name);

                if (existingNote.Any())
                {
                    _logger.LogWarning("A note with the name '{Name}' already exists.", note.Name);
                    return new Result<Note>(false);
                }

                await _noteRepository.AddAsync(note);
                await _unitOfWork.Save();

                _logger.LogInformation("Note '{Name}' added successfully.", note.Name);
                return new Result<Note>(true, note);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while adding note: {Message}", ex.Message);
                return new Result<Note>(false);
            }
        }

        public async Task<Result> EditNote(Note updatedNote)
        {
            _logger.LogInformation("Attempting to edit note with ID {Id}.", updatedNote.Id);

            if (updatedNote == null || string.IsNullOrWhiteSpace(updatedNote.Name))
            {
                _logger.LogWarning("Failed to edit note - invalid data provided.");
                return new Result(false);
            }

            try
            {
                var existingNote = await _noteRepository.GetByIdAsync(updatedNote.Id);

                if (existingNote == null)
                {
                    _logger.LogWarning("Failed to edit note - note with ID {Id} not found.", updatedNote.Id);
                    return new Result(false);
                }

                var duplicateNote = (await _noteRepository.GetAsync(n => n.Name == updatedNote.Name && n.Id != updatedNote.Id)).FirstOrDefault();

                if (duplicateNote != null)
                {
                    _logger.LogWarning("Failed to edit note - note with the name '{Name}' already exists.", updatedNote.Name);
                    return new Result(false);
                }

                existingNote.Name = updatedNote.Name;
                await _noteRepository.UpdateAsync(existingNote);
                await _unitOfWork.Save();

                _logger.LogInformation("Note with ID {Id} updated successfully.", updatedNote.Id);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while editing note with ID {Id}: {Message}", updatedNote.Id, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result> DeleteNote(int noteId)
        {
            _logger.LogInformation("Attempting to delete note with ID {Id}.", noteId);

            if (noteId <= 0)
            {
                _logger.LogWarning("Failed to delete note - invalid note ID provided.");
                return new Result(false);
            }

            try
            {
                var existingNote = await _noteRepository.GetByIdAsync(noteId);

                if (existingNote == null)
                {
                    _logger.LogWarning("Failed to delete note - note with ID {Id} not found.", noteId);
                    return new Result(false);
                }

                var isNoteInUse = (await _unitOfWork.ElixirCompositionRepository
                    .GetAsync(ec => ec.NoteId == noteId)).Any();

                if (isNoteInUse)
                {
                    _logger.LogWarning("Failed to delete note with ID {Id} - note is in use in an Elixir.", noteId);
                    return new Result(false);
                }

                await _noteRepository.DeleteAsync(existingNote);
                await _unitOfWork.Save();

                _logger.LogInformation("Note with ID {Id} deleted successfully.", noteId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while deleting note with ID {Id}: {Message}", noteId, ex.Message);
                return new Result(false);
            }
        }

    }
}
