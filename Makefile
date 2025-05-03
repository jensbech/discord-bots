# Makefile

BW_PROJECT_NAME=BoredGods.dev
BW_PROJECT_ID=9d72d613-8d1f-4455-8676-b2c10167aa94
BW_DOMAIN=https://vault.bitwarden.eu

run:
	@if ! command -v bws >/dev/null 2>&1; then \
		echo "Error: bws is not installed."; \
		exit 1; \
	fi

	@if [ -z "$$BWS_ACCESS_TOKEN" ]; then \
		echo "Error: BWS_ACCESS_TOKEN environment variable is not set."; \
		exit 1; \
	fi

	@bws config server-base $(BW_DOMAIN)
	@echo "Set EU connection $(BW_DOMAIN)"

	@echo "Starting app with project ${BW_PROJECT_NAME}"
	@bws run --project-id $(BW_PROJECT_ID) -- bun src/index.ts
