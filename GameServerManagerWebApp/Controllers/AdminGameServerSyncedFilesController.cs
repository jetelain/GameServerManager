using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameServerManagerWebApp.Entites;
using Microsoft.AspNetCore.Authorization;

namespace GameServerManagerWebApp.Controllers
{
    [Authorize(Policy = "Admin")]
    public class AdminGameServerSyncedFilesController : Controller
    {
        private readonly GameServerManagerContext _context;

        public AdminGameServerSyncedFilesController(GameServerManagerContext context)
        {
            _context = context;
        }

        // GET: AdminGameServerSyncedFiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServerSyncedFile = await _context.GameServerSyncedFile
                .Include(g => g.GameServer)
                .FirstOrDefaultAsync(m => m.GameServerSyncedFileID == id);
            if (gameServerSyncedFile == null)
            {
                return NotFound();
            }

            return View(gameServerSyncedFile);
        }

        // GET: AdminGameServerSyncedFiles/Create
        public async Task<IActionResult> Create(int gameServerID)
        {
            var gameServerSyncedFile = new GameServerSyncedFile();
            gameServerSyncedFile.GameServerID = gameServerID;
            gameServerSyncedFile.GameServer = await _context.GameServers.FirstOrDefaultAsync(m => m.GameServerID == gameServerSyncedFile.GameServerID);
            return View(gameServerSyncedFile);
        }

        // POST: AdminGameServerSyncedFiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GameServerSyncedFileID,GameServerID,Path,SyncUri,Content,LastChangeUTC")] GameServerSyncedFile gameServerSyncedFile)
        {
            if (ModelState.IsValid)
            {
                gameServerSyncedFile.LastChangeUTC = DateTime.MinValue;
                gameServerSyncedFile.Content = null;
                _context.Add(gameServerSyncedFile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminGameServersController.Details), "AdminGameServers", new { id = gameServerSyncedFile.GameServerID });
            }
            gameServerSyncedFile.GameServer = await _context.GameServers.FirstOrDefaultAsync(m => m.GameServerID == gameServerSyncedFile.GameServerID);
            return View(gameServerSyncedFile);
        }

        // GET: AdminGameServerSyncedFiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServerSyncedFile = await _context.GameServerSyncedFile
                .Include(g => g.GameServer)
                .FirstOrDefaultAsync(m => m.GameServerSyncedFileID == id);
            if (gameServerSyncedFile == null)
            {
                return NotFound();
            }
            return View(gameServerSyncedFile);
        }

        // POST: AdminGameServerSyncedFiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GameServerSyncedFileID,GameServerID,Path,SyncUri,Content,LastChangeUTC")] GameServerSyncedFile gameServerSyncedFile)
        {
            if (id != gameServerSyncedFile.GameServerSyncedFileID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    gameServerSyncedFile.LastChangeUTC = DateTime.MinValue;
                    gameServerSyncedFile.Content = null;
                    _context.Update(gameServerSyncedFile);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameServerSyncedFileExists(gameServerSyncedFile.GameServerSyncedFileID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(AdminGameServersController.Details), "AdminGameServers", new { id = gameServerSyncedFile.GameServerID });
            }
            gameServerSyncedFile.GameServer = await _context.GameServers.FirstOrDefaultAsync(m => m.GameServerID == gameServerSyncedFile.GameServerID);
            return View(gameServerSyncedFile);
        }

        // GET: AdminGameServerSyncedFiles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameServerSyncedFile = await _context.GameServerSyncedFile
                .Include(g => g.GameServer)
                .FirstOrDefaultAsync(m => m.GameServerSyncedFileID == id);
            if (gameServerSyncedFile == null)
            {
                return NotFound();
            }

            return View(gameServerSyncedFile);
        }

        // POST: AdminGameServerSyncedFiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gameServerSyncedFile = await _context.GameServerSyncedFile.FindAsync(id);
            _context.GameServerSyncedFile.Remove(gameServerSyncedFile);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminGameServersController.Details), "AdminGameServers", new { id = gameServerSyncedFile.GameServerID });
        }

        private bool GameServerSyncedFileExists(int id)
        {
            return _context.GameServerSyncedFile.Any(e => e.GameServerSyncedFileID == id);
        }
    }
}
