# Configuration Files

This folder contains sensitive configuration files that should NOT be committed to the repository.

## Files to Place Here

### GitHub App Private Key
- **File**: `gexvisor-projects-sync.*.private-key.pem`
- **Purpose**: Authenticates the GitHub App for Projects v2 API access
- **How to get**: GitHub Settings > Developer settings > GitHub Apps > Your App > Generate private key

### Future Files
- `appsettings.local.json` - Local environment overrides
- `*.env` - Environment variable files
- API keys, tokens, credentials

## Security Notes

- All files in this folder (except this README) are gitignored
- Never commit `.pem` or `.key` files to the repository
- If you accidentally commit a private key, revoke it immediately and generate a new one
