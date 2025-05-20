from pydantic_ai.models import Model


def is_person_safe_name(name: str, model: Model) -> bool:
    """
    The purpose of the game is to have fun,
    so let's filter out names that would not be appropriate or not in the right spirit.
    """
    return False
