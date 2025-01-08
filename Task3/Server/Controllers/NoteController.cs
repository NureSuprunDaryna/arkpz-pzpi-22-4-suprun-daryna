using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.ViewModels.Note;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoteController : ControllerBase
    {
        private readonly ILogger<NoteController> _logger;
        private readonly INoteService _noteService;
        private readonly IHistoryService _historyService;
        private readonly UserManager<AppUser> _userManager;

        public NoteController(ILogger<NoteController> logger, INoteService noteService, IHistoryService historyService, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _noteService = noteService;
            _historyService = historyService;
            _userManager = userManager;
        }

        //Створення ноти (тільки адмін)
        [HttpPost("create")]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogError("Failed to create note - invalid input data.");
                return BadRequest(new { message = "Note name is required." });
            }

            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to create note.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", currentUserId);
                return Unauthorized(new { message = "User not found." });
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("User {UserName} does not have admin privileges.", currentUser.UserName);
                return Forbid();
            }

            _logger.LogInformation("Admin {AdminName} is attempting to create a new note: {NoteName}.", currentUser.UserName, model.Name);

            var note = new Note
            {
                Name = model.Name
            };

            var result = await _noteService.CreateNote(note);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to create note with name: {NoteName}.", model.Name);
                return BadRequest(new { message = "Failed to create note." });
            }

            _logger.LogInformation("Note created successfully with ID {NoteId}.", result.Data.Id);
            return Ok(new { message = "Note created successfully.", noteId = result.Data.Id });
        }


        // Оновлення ноти (тільки адмін)
        [HttpPut("update")]
        public async Task<IActionResult> UpdateNote([FromBody] UpdateNoteModel model)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to update a note.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", currentUserId);
                return Unauthorized(new { message = "User not found." });
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("User {UserName} does not have admin privileges.", currentUser.UserName);
                return Forbid();
            }

            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogError("Failed to update a note - invalid input data.");
                return BadRequest(new { message = "Valid note ID and name are required." });
            }

            _logger.LogInformation("Admin {AdminName} is attempting to update note with ID {NoteId}.", currentUser.UserName, model.Id);

            var note = new Note();
            note.MapFrom(model);

            var result = await _noteService.EditNote(note);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to update note with ID {NoteId}.", model.Id);
                return BadRequest(new { message = "Failed to update note." });
            }

            _logger.LogInformation("Note with ID {NoteId} updated successfully.", model.Id);
            return Ok(new { message = "Note updated successfully." });
        }

        // Видалення ноти (тільки адмін)
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            if (id <= 0)
            {
                _logger.LogError("Invalid note ID: {NoteId}.", id);
                return BadRequest(new { message = "Invalid note ID." });
            }

            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to delete a note.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", currentUserId);
                return Unauthorized(new { message = "User not found." });
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("User {UserName} does not have admin privileges.", currentUser.UserName);
                return Forbid();
            }

            _logger.LogInformation("Admin {AdminName} is attempting to delete note with ID {NoteId}.", currentUser.UserName, id);

            var result = await _noteService.DeleteNote(id);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to delete note with ID {NoteId}.", id);
                return BadRequest(new { message = "Failed to delete note." });
            }

            _logger.LogInformation("Note with ID {NoteId} deleted successfully by admin {AdminName}.", id, currentUser.UserName);
            return Ok(new { message = "Note deleted successfully." });
        }

        // Отримання списку нот
        [HttpGet("list")]
        public async Task<IActionResult> GetAllNotes()
        {
            _logger.LogInformation("Retrieving all notes.");

            var result = await _noteService.GetAllNotes();

            if (!result.IsSuccessful || result.Data == null || !result.Data.Any())
            {
                _logger.LogWarning("No notes found.");
                return NotFound(new { message = "No notes available." });
            }

            var notes = result.Data.Select(note =>
            {
                var viewModel = new NoteViewModel();
                viewModel.MapFrom(note);
                return viewModel;
            }).ToList();

            _logger.LogInformation("Successfully retrieved notes.");
            return Ok(notes);
        }

        // Додавання нот до еліксиру
        [HttpPost("add-to-elixir/{elixirId}")]
        public async Task<IActionResult> AddNotesToElixir(int elixirId, [FromBody] List<AddNoteToElixirModel> models)
        {
            if (models == null || !models.Any())
            {
                _logger.LogError("Failed to add notes to elixir - no notes data provided.");
                return BadRequest(new { message = "Notes data is required." });
            }

            _logger.LogInformation("Attempting to add notes to elixir with ID {ElixirId}.", elixirId);

            var notesData = models.Select(model => (model.Id, model.NoteCategory, model.Proportion)).ToList();

            var result = await _noteService.AddNotesToElixir(elixirId, notesData);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to add notes to elixir with ID {ElixirId}.", elixirId);
                return BadRequest(new { message = "Failed to add notes to elixir." });
            }

            _logger.LogInformation("Notes added to elixir with ID {ElixirId} successfully.", elixirId);
            return Ok(new { message = "Notes added to elixir successfully." });
        }
    }
}
