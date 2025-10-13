My app for hosting Discord bots.

Discord commands:
- `/ask` about source material from the wiki. Article results from initial key word query is fed into AI to provide the user with an accurate answer.
- `/rules` to query AI about DnD rules.
- `/roll` dice (e.g. `/roll 2d20+3`).
- `/help`

Webhook:
- Posts to Discord channel every time a new Wiki article is created.

-----

Required environment variables:
- `DISCORD_APP_APPLICATION_ID`
- `BOOKSTACK_API_ID`
- `BOOKSTACK_API_KEY`
- `BOOKSTACK_BASE_URL`
- `DISCORD_BOT_TOKEN`
- `GUILD_ID`
- `OPENAI_API_KEY`
- `OPENAI_PROJECT`


Stage environment secrets with Bitwarden Secrets Manager CLI
- Set BitWarden server: `bws config server-base https://vault.bitwarden.eu`.
- Set token into Keychain: `security add-generic-password -a "bws_access_token" -s "Bitwarden Secrets Manager" -w "your_access_token_here"`
- Get `BWS_ACCESS_TOKEN` from Keychain: `export BWS_ACCESS_TOKEN=$(security find-generic-password -a "bws_access_token" -s "Bitwarden Secrets Manager" -w)`

Run:
- `bws run --project-id $BORED_GODS_DEV_BWS_PROJECT_ID -- just run`
- `bws run --project-id $BORED_GODS_DEV_BWS_PROJECT_ID -- docker compose up -d`

On Docker Hub: https://hub.docker.com/r/jensbech/discord-bots

# Todo:
- Rename env vars for more clarity, update all necessary places.
- Implement webhook feature.
