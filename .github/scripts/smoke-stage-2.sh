#!/usr/bin/env bash
# Приёмка этапа 2 на поднятом docker compose: вход по имени заводит рабочее
# пространство, а в сетке недели появляется витринная встреча с макета.
set -euo pipefail

API="${API:-http://localhost:8080/api}"
NAME="${NAME:-Приёмщик CI}"

session=$(curl --fail --silent --show-error \
  -H 'Content-Type: application/json' \
  -d "{\"name\":\"${NAME}\",\"timeZoneId\":\"Asia/Yekaterinburg\",\"utcOffsetMinutes\":300}" \
  "${API}/session")

user_id=$(echo "${session}" | python3 -c 'import json,sys; print(json.load(sys.stdin)["id"])')
echo "Вошли как ${NAME}: ${user_id}"

# Понедельник текущей недели в поясе стенда — так же считает наполнение демо-данными.
read -r from to < <(python3 - "$user_id" <<'PY'
from datetime import datetime, timedelta, timezone
offset = timedelta(minutes=300)
local = datetime.now(timezone.utc) + offset
monday = local.replace(hour=0, minute=0, second=0, microsecond=0) - timedelta(days=local.weekday())
start = monday - offset
print(start.strftime('%Y-%m-%dT%H:%M:%SZ'), (start + timedelta(days=7)).strftime('%Y-%m-%dT%H:%M:%SZ'))
PY
)

calendars=$(curl --fail --silent --show-error -H "X-Talkaton-User: ${user_id}" "${API}/calendars")
echo "${calendars}" | python3 -c '
import json, sys
calendars = json.load(sys.stdin)
assert len(calendars) == 4, f"ожидали 4 календаря, получили {len(calendars)}"
print("Календари:", ", ".join(c["name"] for c in calendars))
'

events=$(curl --fail --silent --show-error -H "X-Talkaton-User: ${user_id}" \
  "${API}/events?from=${from}&to=${to}")

echo "${events}" | python3 -c '
import json, sys
events = json.load(sys.stdin)
assert events, "сетка недели пустая"
showcase = [e for e in events if e["title"] == "Штаб Платформы Данных"]
assert showcase, "витринной встречи с макета нет в сетке"
assert showcase[0]["recurrenceRule"] == "FREQ=WEEKLY;BYDAY=WE", showcase[0]["recurrenceRule"]
assert showcase[0]["artifactCount"] == 4, showcase[0]["artifactCount"]
print(f"Встреч на неделе: {len(events)}, витринная на месте")
'
