# Set up env and run

- Set Bitwarden server: `bws config server-base https://vault.bitwarden.eu`.
- Set token into Keychain: `security add-generic-password -a "bws_access_token" -s "Bitwarden Secrets Manager" -w "your_access_token_here"`
- Get environment variable from Keychain: `export BWS_ACCESS_TOKEN=$(security find-generic-password -a "bws_access_token" -s "Bitwarden Secrets Manager" -w)`

Dynamically insert all secrets and run dev server: `bws run --project-id $BORED_GODS_DEV_BWS_PROJECT_ID -- just run`
