using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuideXOS;
using Microsoft.AspNetCore.Mvc;

namespace guideXOS.Web.Controllers
{
    [ApiController]
    public class CloudFsController : ControllerBase
    {
        private readonly Repository _repo = new Repository();

        private bool TryGetLoginGuid(out Guid token)
        {
            token = Guid.Empty;
            string auth = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(auth)) return false;
            if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
            string val = auth.Substring(7).Trim();
            Guid g; if (!Guid.TryParse(val, out g)) return false;
            token = g; return _repo.ValidateToken(g);
        }

        [HttpGet]
        [Route("v1/fs/entries")]
        public IActionResult List([FromQuery] string path = "", [FromQuery] bool recursive = false, [FromQuery] int pageSize = 1000, [FromQuery] string continuationToken = null)
        {
            if (!TryGetLoginGuid(out Guid token)) return Unauthorized();
            var all = _repo.ListDesktopFiles(token);
            if (path == null) path = string.Empty;
            string norm = path;
            if (norm.StartsWith("/")) norm = norm.Substring(1);
            if (norm.Length > 0 && norm[norm.Length - 1] != '/') norm += "/";

            var items = new List<object>();
            var seenDirs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in all)
            {
                if (!f.FileName.StartsWith(norm, StringComparison.Ordinal)) continue;
                var rest = f.FileName.Substring(norm.Length);
                int slash = rest.IndexOf('/');
                if (slash >= 0)
                {
                    string dir = rest.Substring(0, slash);
                    if (seenDirs.Add(dir)) items.Add(new { path = "/" + norm + dir, name = dir, type = "Directory" });
                    if (!recursive) continue;
                }
                if (slash < 0)
                {
                    items.Add(new { path = "/" + norm + rest, name = rest, type = "File" });
                }
            }
            return Ok(new { items, continuationToken = (string)null });
        }

        [HttpGet]
        [Route("v1/fs/files/content")]
        public IActionResult Read([FromQuery] string path)
        {
            if (!TryGetLoginGuid(out Guid token)) return Unauthorized();
            if (string.IsNullOrEmpty(path)) return BadRequest();
            string norm = path.StartsWith("/") ? path.Substring(1) : path;
            var f = _repo.GetDesktopFile(token, norm);
            if (f == null) return NotFound();
            var bytes = Encoding.UTF8.GetBytes(f.Content ?? string.Empty);
            return File(bytes, "application/octet-stream");
        }

        [HttpPut]
        [Route("v1/fs/files/content")]
        public IActionResult Write([FromQuery] string path, [FromQuery] bool createParents = true, [FromBody] byte[] content = null)
        {
            if (!TryGetLoginGuid(out Guid token)) return Unauthorized();
            if (string.IsNullOrEmpty(path)) return BadRequest();
            string norm = path.StartsWith("/") ? path.Substring(1) : path;
            string text = content != null ? Encoding.UTF8.GetString(content) : string.Empty;
            _repo.SaveDesktopText(token, norm, text);
            return Ok(new { path, size = (long)(content?.Length ?? 0), eTag = string.Empty });
        }

        [HttpDelete]
        [Route("v1/fs/entries")]
        public IActionResult Delete([FromQuery] string path)
        {
            if (!TryGetLoginGuid(out Guid token)) return Unauthorized();
            if (string.IsNullOrEmpty(path)) return BadRequest();
            string norm = path.StartsWith("/") ? path.Substring(1) : path;
            if (norm.EndsWith("/")) _repo.DeleteByPrefix(token, norm);
            else _repo.DeleteDesktopFile(token, norm);
            return NoContent();
        }
    }
}
