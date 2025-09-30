This is an app for my Discord bots.

Discord commands:
- `/search` articles from our [Bored Gods Wiki](https://wiki.boredgods.no/) (D&D campaign).
- `/rules` to query AI about DnD rules.
- `/ask` about source material from the wiki. Article results from initial key word query is fed into AI to provide the user with an accurate answer.
- `/roll` dice (e.g. `/roll 2d20+3`).

Webhook:
- Posts to Discord channel every time a new Wiki article is created.

-----

Used environment variables:
- `APPLICATION_ID`
- `BOOKSTACK_API_ID`
- `BOOKSTACK_API_KEY`
- `BOOKSTACK_BASE_URL`
- `DISCORD_BOT_TOKEN`
- `GUILD_ID`
- `OPENAI_API_KEY`
- `OPENAI_PROJECT`

Stage environment secrets with Bitwarden Secrets Manager
- Set BitWarden server: `bws config server-base https://vault.bitwarden.eu`.
- Set token into Keychain: `security add-generic-password -a "bws_access_token" -s "Bitwarden Secrets Manager" -w "your_access_token_here"`
- Get `BWS_ACCESS_TOKEN` from Keychain: `export BWS_ACCESS_TOKEN=$(security find-generic-password -a "bws_access_token" -s "Bitwarden Secrets Manager" -w)`

Run:
- `bws run --project-id $BORED_GODS_DEV_BWS_PROJECT_ID -- just run`
- `bws run --project-id $BORED_GODS_DEV_BWS_PROJECT_ID -- docker compose up -d`

On Docker Hub: https://hub.docker.com/r/jensbech/bored-bot