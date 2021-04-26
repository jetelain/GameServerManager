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
    public class AdminHostServersController : Controller
    {
        private readonly GameServerManagerContext _context;

        public AdminHostServersController(GameServerManagerContext context)
        {
            _context = context;
        }

        // GET: AdminHostServers
        public async Task<IActionResult> Index()
        {
            return View(await _context.HostServers.ToListAsync());
        }

        // GET: AdminHostServers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hostServer = await _context.HostServers
                .FirstOrDefaultAsync(m => m.HostServerID == id);
            if (hostServer == null)
            {
                return NotFound();
            }

            return View(hostServer);
        }

        // GET: AdminHostServers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminHostServers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HostServerID,Name,Address,SshUserName")] HostServer hostServer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hostServer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hostServer);
        }

        // GET: AdminHostServers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hostServer = await _context.HostServers.FindAsync(id);
            if (hostServer == null)
            {
                return NotFound();
            }
            return View(hostServer);
        }

        // POST: AdminHostServers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HostServerID,Name,Address,SshUserName")] HostServer hostServer)
        {
            if (id != hostServer.HostServerID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hostServer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HostServerExists(hostServer.HostServerID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(hostServer);
        }

        // GET: AdminHostServers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hostServer = await _context.HostServers
                .FirstOrDefaultAsync(m => m.HostServerID == id);
            if (hostServer == null)
            {
                return NotFound();
            }

            return View(hostServer);
        }

        // POST: AdminHostServers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hostServer = await _context.HostServers.FindAsync(id);
            _context.HostServers.Remove(hostServer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HostServerExists(int id)
        {
            return _context.HostServers.Any(e => e.HostServerID == id);
        }
    }
}
