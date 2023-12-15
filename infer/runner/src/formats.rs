use chat::{ChatTemplate, FunctionStyle};

pub fn llama2_chat() -> ChatTemplate {
    ChatTemplate::new(
        "{% if messages[0]['role'] == 'system' %}{% set loop_messages = messages[1:] %}{% set system_message = messages[0]['content'] %}{% else %}{% set loop_messages = messages %}{% set system_message = false %}{% endif %}{% for message in loop_messages %}{% if loop.index0 == 0 and system_message != false %}{% set content = '<<SYS>>\n' + system_message + '\n<</SYS>>\n\n' + message['content'] %}{% else %}{% set content = message['content'] %}{% endif %}{% if message['role'] == 'user' %}{{ bos_token + '[INST] ' + content + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ ' '  + content + eos_token }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::SystemPrompt
    )
}

pub fn mistral_instruct() -> ChatTemplate {
    ChatTemplate::new(
        "{{ bos_token }}{% for message in messages %}{% if message['role'] == 'user' %}{{ '[INST] ' + message['content'] + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ ' ' + message['content'] + eos_token }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        true,
        FunctionStyle::SystemPrompt
    )
}

#[allow(dead_code)]
pub fn airoboros() -> ChatTemplate {
    ChatTemplate::new(
        "{% if messages[0]['role'] == 'system' %}{% set loop_messages = messages[1:] %}{% set system_message = messages[0]['content'] %}{% else %}{% set loop_messages = messages %}{% set system_message = false %}{% endif %}{% for message in loop_messages %}{% if loop.index0 == 0 and system_message != false %}{% set content = '<<SYS>>\n' + system_message + '\n<</SYS>>\n\n' + message['content'] %}{% else %}{% set content = message['content'] %}{% endif %}{% if message['role'] == 'user' %}{{ bos_token + '[INST] ' + content + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ ' '  + content + ' ' + eos_token }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::SystemPrompt
    )
}

pub fn synthia() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'system' %}{% set role = 'SYSTEM' %}{% elif message['role'] == 'user' %}{% set role = 'USER' %}{% elif message['role'] == 'assistant' %}{% set role = 'ASSISTANT' %}{% else %}{% set role = message['role'] %}{% endif %}{{ role + ': ' + message['content'] + '\n'}}{% endfor %}",
        "",
        "</s>",
        false,
        false, FunctionStyle::SystemPrompt
    )
}

pub fn zephyr() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ '<|user|>\n' + message['content'] + eos_token }}{% elif message['role'] == 'system' %}{{ '<|system|>\n' + message['content'] + eos_token }}{% elif message['role'] == 'assistant' %}{{ '<|assistant|>\n'  + message['content'] + eos_token }}{% endif %}\n{% if loop.last and add_generation_prompt %}{{ '<|assistant|>' }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn dolphin() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn hermes() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn orca2() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn neural() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ '### User:\n' + message['content'] }}{% elif message['role'] == 'system' %}{{ '### System:\n' + message['content'] }}{% elif message['role'] == 'assistant' %}{{ '### Assistant:\n'  + message['content'] }}{% endif %}\n{% if loop.last and add_generation_prompt %}{{ '### Assistant:' }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn grendel() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn openchat() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ 'GPT4 Correct User: ' + message['content'] + eos_token }}{% elif message['role'] == 'system' %}{{ 'System: ' + message['content'] + eos_token }}{% elif message['role'] == 'assistant' %}{{ 'GPT4 Correct Assistant: '  + message['content'] + eos_token }}{% endif %}{% if loop.last and add_generation_prompt %}{{ 'GPT4 Correct Assistant:' }}{% endif %}{% endfor %}",
        "<s>",
        "<|end_of_turn|>",
        true,
        false,
        FunctionStyle::SystemPrompt
    )
}

pub fn starling() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ 'GPT4 Correct User: ' + message['content'] + eos_token }}{% elif message['role'] == 'system' %}{{ 'System: ' + message['content'] + eos_token }}{% elif message['role'] == 'assistant' %}{{ 'GPT4 Correct Assistant: '  + message['content'] + eos_token }}{% endif %}{% if loop.last and add_generation_prompt %}{{ 'GPT4 Correct Assistant:' }}{% endif %}{% endfor %}",
        "<s>",
        "<|end_of_turn|>",
        true,
        false,
        FunctionStyle::SystemPrompt
    )
}

/// Bad responses? Did not understand how to call functions at all.
pub fn amazon_mistral_lite() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ '<|prompter|>' + message['content'] + eos_token }}{% elif message['role'] == 'system' %}{{ '<|prompter|>' + message['content'] + eos_token + '<|assistant|>understood!' + eos_token }}{% elif message['role'] == 'assistant' %}{{ '<|assistant|>'  + message['content'] + eos_token }}{% endif %}{% if loop.last and add_generation_prompt %}{{ '<|assistant|>' }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

/// Bad responses? Did not understand how to call functions at all.
pub fn yi_capybara_nous() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ 'USER: ' + message['content'] + ' ' }}{% elif message['role'] == 'system' %}{{ 'USER: ' + message['content']  + ' ASSISTANT: understood! ' }}{% elif message['role'] == 'assistant' %}{{ 'ASSISTANT: '  + message['content'] }}{% endif %}{% if loop.last and add_generation_prompt %}{{ 'ASSISTANT:' }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

/// Refuses to answer questions about anything other than programming,
/// even with a system prompt to guide it
pub fn deepseek_coder() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'user' %}{{ '### Instruction:\n' + message['content'] + '\n' }}{% elif message['role'] == 'system' %}{{ message['content'] + '\n' }}{% elif message['role'] == 'assistant' %}{{ '### Response:\n'  + message['content'] + '\n' + eos_token + '\n' }}{% endif %}{% if loop.last and add_generation_prompt %}{{ '### Response:' }}{% endif %}{% endfor %}",
        "<s>",
        "<|EOT|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

pub fn mistral_slimorcaboros() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}
