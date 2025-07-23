#!/bin/sh

# CGI header
echo "Content-Type: text/html"
echo

# HTML output
cat <<EOF
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>CGI Environment Variables</title>
    <style>
        body { font-family: monospace; background: #f9f9f9; padding: 1em; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ccc; padding: 4px 8px; text-align: left; }
        th { background: #eee; }
    </style>
</head>
<body>
    <h1>CGI Environment Variables</h1>
    <table>
        <thead>
            <tr><th>Variable</th><th>Value</th></tr>
        </thead>
        <tbody>
EOF

# Output all environment variables
env | sort | while IFS='=' read -r key value; do
    # HTML-escape &, <, > for safety
    escaped_value=$(echo "$value" | sed 's/&/&amp;/g; s/</\&lt;/g; s/>/\&gt;/g')
    printf '<tr><td>%s</td><td>%s</td></tr>\n' "$key" "$escaped_value"
done

cat <<EOF
        </tbody>
    </table>
</body>
</html>
EOF
