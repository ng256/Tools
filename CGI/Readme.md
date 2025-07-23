# 🧭 CGI Environment Variables Cheat Sheet

This cheat sheet lists standard environment variables passed by the web server to a CGI script.

## 📌 Common Variables

| Variable             | Description |
|----------------------|-------------|
| `REQUEST_METHOD`     | HTTP method used for the request (`GET`, `POST`, `HEAD`, `PUT`, etc.). |
| `QUERY_STRING`       | The part of the URL after `?`, used in `GET`/`HEAD` requests. |
| `CONTENT_LENGTH`     | Size of the request body (in bytes) for `POST`/`PUT`. |
| `CONTENT_TYPE`       | MIME type of the request body (`application/x-www-form-urlencoded`, `multipart/form-data`, etc.). |
| `SCRIPT_NAME`        | Path to the CGI script relative to the web root. |
| `PATH_INFO`          | Extra path info after script name, used in REST-style URLs. |
| `PATH_TRANSLATED`    | Filesystem path corresponding to `PATH_INFO`. |
| `SERVER_NAME`        | Server host name (e.g., `example.com`). |
| `SERVER_PORT`        | Port number of the server (`80`, `443`, etc.). |
| `SERVER_PROTOCOL`    | Protocol version (e.g., `HTTP/1.1`, `HTTP/2.0`). |
| `SERVER_SOFTWARE`    | Server software version string (e.g., `uhttpd`, `Apache/2.4`). |
| `REMOTE_ADDR`        | IP address of the client. |
| `REMOTE_HOST`        | Hostname of the client (may not be available). |
| `REMOTE_PORT`        | Port number used by the client. |
| `GATEWAY_INTERFACE`  | CGI interface version (typically `CGI/1.1`). |

## 🔐 Authentication Variables

| Variable             | Description |
|----------------------|-------------|
| `AUTH_TYPE`          | Authentication type (`Basic`, `Digest`, etc.). |
| `REMOTE_USER`        | Username of the authenticated user. |
| `REMOTE_IDENT`       | User identity from ident protocol (rarely used). |
| `HTTP_AUTHORIZATION` | Raw `Authorization` header (not always available in CGI; needs server config). |

## 🌐 HTTP Headers as Environment Variables

HTTP headers are converted to environment variables with:

- Prefix `HTTP_`
- All letters uppercased
- Hyphens (`-`) replaced with underscores (`_`)

Examples:

| HTTP Header        | CGI Variable          |
|--------------------|-----------------------|
| `User-Agent`       | `HTTP_USER_AGENT`     |
| `Accept`           | `HTTP_ACCEPT`         |
| `Accept-Encoding`  | `HTTP_ACCEPT_ENCODING`|
| `Accept-Language`  | `HTTP_ACCEPT_LANGUAGE`|
| `Host`             | `HTTP_HOST`           |
| `Referer`          | `HTTP_REFERER`        |
| `Cookie`           | `HTTP_COOKIE`         |

## 📂 Additional (Non-standard) Variables

| Variable             | Description |
|----------------------|-------------|
| `DOCUMENT_ROOT`      | Filesystem path to the site root. |
| `SCRIPT_FILENAME`    | Full path to the CGI script. |
| `REQUEST_URI`        | Original request URI (including `QUERY_STRING`). |
| `HTTPS`              | Often set to `on` when request is over HTTPS. |

## 🧪 Examples

### Dump all CGI environment variables (POSIX shell):

```sh
#!/bin/sh
echo "Content-Type: text/plain"
echo
env | grep -E '^(REQUEST|REMOTE|SERVER|HTTP_|PATH|AUTH|CONTENT|GATEWAY)' | sort
```

### Get authenticated username:

```sh
#!/bin/sh
echo "Content-Type: text/plain"
echo
echo "Hello, $REMOTE_USER"
```

### Parse `QUERY_STRING` manually:

```sh
#!/bin/sh
echo "Content-Type: text/plain"
echo

# Example: QUERY_STRING="name=John&age=42"
IFS='&' read -r -a params <<< "$QUERY_STRING"

for pair in "${params[@]}"; do
    IFS='=' read -r key value <<< "$pair"
    printf "%s = %s\n" "$key" "$value"
done
```

> Note: POSIX `sh` on many systems doesn’t support arrays. The above works in `bash`. For strict POSIX shell, use `while` loops and `cut`.

## ⚠️ Security Tips

- Always **validate and sanitize** user input.
- Never trust `HTTP_*` headers directly.
- Use `HTTPS` with `Basic` authentication.
- Avoid hardcoded passwords inside CGI scripts.
- Do not expose debugging output on production systems.

## 📎 Test with curl

```sh
# Basic Auth with curl
curl -u username:password http://host/cgi-bin/myscript.cgi

# Or using the Authorization header directly
curl -H "Authorization: Basic dXNlcjpwYXNz" http://host/cgi-bin/myscript.cgi
```

---
