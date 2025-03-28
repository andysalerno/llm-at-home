import time


class CachingPromptProvider:
    def __init__(
        self,
        system_prompt_path="system_prompt.txt",
        vibe_path="vibe.txt",
        expiration_seconds=60,
    ):
        self.base_prompt_path = system_prompt_path
        self.vibe_path = vibe_path
        self.expiration_seconds = expiration_seconds
        self._cached_system_prompt = None
        self._cached_vibe = None
        self._last_update = 0

    def _refresh_prompts(self):
        with open(self.base_prompt_path, "r") as f:
            self._cached_system_prompt = f.read()
        with open(self.vibe_path, "r") as f:
            self._cached_vibe = f.read().strip()

        print("Base prompt and vibe loaded from files.", flush=True)

    def get_system_prompt(self) -> str:
        current_time = time.time()
        if (
            self._cached_system_prompt is None
            or (current_time - self._last_update) > self.expiration_seconds
        ):
            self._refresh_prompts()
            self._last_update = current_time

        if self._cached_system_prompt is None:
            raise ValueError("System prompt is not loaded.")

        return self._cached_system_prompt

    def get_vibe_rule(self):
        current_time = time.time()
        if (
            self._cached_vibe is None
            or (current_time - self._last_update) > self.expiration_seconds
        ):
            self._refresh_prompts()
            self._last_update = current_time

        if self._cached_vibe is None:
            raise ValueError("Vibe rulre is not loaded.")

        return self._cached_vibe
