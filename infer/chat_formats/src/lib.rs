//! A library providing a variety of chat format templates.
#![allow(clippy::multiple_crate_versions, missing_docs)]
use chat::{ChatTemplate, FunctionStyle};
use log::info;

#[must_use]
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

#[must_use]
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
#[must_use]
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

#[must_use]
pub fn synthia() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'system' %}{% set role = 'SYSTEM' %}{% elif message['role'] == 'user' %}{% set role = 'USER' %}{% elif message['role'] == 'assistant' %}{% set role = 'ASSISTANT' %}{% else %}{% set role = message['role'] %}{% endif %}{{ role + ': ' + message['content'] + '\n'}}{% endfor %}",
        "",
        "</s>",
        false,
        false, FunctionStyle::SystemPrompt
    )
}

#[must_use]
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

#[must_use]
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

#[must_use]
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

#[must_use]
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

#[must_use]
pub fn cloudymixtral() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<|im_start|>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
pub fn fusion_net() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<|im_start|>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
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

#[must_use]
pub fn mixtral_11bx2() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{% if message['role'] == 'system' %}{% if message['content']%}{{'### System:\n' + message['content']+'\n\n'}}{% endif %}{% elif message['role'] == 'user' %}{{'### User:\n' + message['content']+'\n\n'}}{% elif message['role'] == 'assistant' %}{{'### Assistant:\n' + message['content'] + '\n\n'}}{% endif %}{% if loop.last and add_generation_prompt %}{{ '### Assistant:\n' }}{% endif %}{% endfor %}",
        "<s>",
        "</s>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
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

#[must_use]
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

#[must_use]
pub fn laser_dolphin_x2() -> ChatTemplate {
    // ChatTemplate::new(
    //     "{% for message in messages %}{% if message['role'] == 'system' %}{% if message['content']%}{{'### System:\n' + message['content']+'\n\n'}}{% endif %}{% elif message['role'] == 'user' %}{{'### User:\n' + message['content']+'\n\n'}}{% elif message['role'] == 'assistant' %}{{'### Assistant:\n' + message['content'] + '\n\n'}}{% endif %}{% if loop.last and add_generation_prompt %}{{ '### Assistant:\n' }}{% endif %}{% endfor %}",
    //     "<s>",
    //     "</s>",
    //     true,
    //     false,
    //     FunctionStyle::AppendToUserMessage
    // )
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
pub fn pluto() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
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

#[must_use]
pub fn tenyx() -> ChatTemplate {
    ChatTemplate::new(
        "{{ bos_token }} {% for message in messages %}{% if message['role'] == 'user' %}{{ 'User:' + message['content'] + eos_token + '\n' }}{% elif message['role'] == 'system' %}{{ 'System:' + message['content'] + eos_token + '\n' }}{% elif message['role'] == 'assistant' %}{{ 'Assistant:'  + message['content'] + eos_token + '\n' }}{% endif %}{% if loop.last and add_generation_prompt %}{{ 'Assistant:' }}{% endif %}{% endfor %}",
        "<s>",
        "<|end_of_turn|>",
        true,
        false,
        FunctionStyle::SystemPrompt
    )
}

#[must_use]
pub fn nous_hermes_solar() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
pub fn beyonder() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
pub fn neural_hermes() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

#[must_use]
pub fn beagle() -> ChatTemplate {
    ChatTemplate::new(
        "{% for message in messages %}{{ '<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n' }}{% if loop.last and add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}{% endfor %}",
        "<s>",
        "<|im_end|>",
        true,
        false,
        FunctionStyle::AppendToUserMessage
    )
}

/// Bad responses? Did not understand how to call functions at all.
#[must_use]
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
#[must_use]
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
#[must_use]
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

#[must_use]
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

/// Determine the proper `ChatTemplate` given the name of the model.
#[must_use]
pub fn detect_chat_template(model_name: &str) -> ChatTemplate {
    if model_name.contains("CausalLM") {
        info!("Detected turn format: causal_lm");
        todo!()
    } else if model_name.to_lowercase().contains("llama-2") {
        info!("Detected turn format: Llama-2");
        llama2_chat()
    } else if model_name.to_lowercase().contains("fusion") {
        info!("Detected turn format: fusion net");
        fusion_net()
    } else if model_name
        .to_lowercase()
        .contains("andysalerno/mistral-sft")
    {
        info!("Detected turn format: mistral-sft");
        orca2()
    } else if model_name.to_lowercase().contains("rainbowfish") {
        info!("Detected turn format: rainbowfish");
        orca2()
    } else if model_name.to_lowercase().contains("cloudy") {
        info!("Detected turn format: cloudy mixtral");
        cloudymixtral()
    } else if model_name.to_lowercase().contains("pluto") {
        info!("Detected turn format: pluto");
        pluto()
    } else if model_name.to_lowercase().contains("beagle") {
        info!("Detected turn format: beagle");
        beagle()
    } else if model_name.to_lowercase().contains("laser-dolphin-mixtral") {
        info!("Detected turn format: laser_dolphin");
        laser_dolphin_x2()
    } else if model_name.to_lowercase().contains("tenyx") {
        info!("Detected turn format: tenyx");
        tenyx()
    } else if model_name.to_lowercase().contains("beyonder") {
        info!("Detected turn format: Beyonder");
        beyonder()
    } else if model_name.to_lowercase().contains("neuralhermes") {
        info!("Detected turn format: neural hermes");
        neural_hermes()
    } else if model_name.to_lowercase().contains("mixtral_11bx2") {
        info!("Detected turn format: mixtral11bx2");
        mixtral_11bx2()
    } else if model_name.to_lowercase().contains("mistral-7b-instruct") {
        info!("Detected turn format: mistral-instruct");
        mistral_instruct()
    } else if model_name.to_lowercase().contains("solar") {
        info!("Detected turn format: nous-hermes-solar");
        nous_hermes_solar()
    } else if model_name.to_lowercase().contains("/data") {
        info!("Detected turn format: mistral-instruct");
        mistral_instruct()
    } else if model_name.contains("zephyr") {
        info!("Detected turn format: zephyr");
        zephyr()
    } else if model_name.contains("dolphin") {
        info!("Detected turn format: dolphin");
        dolphin()
    } else if model_name.contains("openhermes") || model_name.contains("skywork") {
        info!("Detected turn format: chatml");
        todo!()
    } else if model_name.contains("agentlm") {
        info!("Detected turn format: agentlm");
        llama2_chat()
    } else if model_name.contains("openchat") {
        info!("Detected turn format: openchat");
        openchat()
    } else if model_name.to_lowercase().contains("starling") {
        info!("Detected turn format: starling (openchat)");
        starling()
    } else if model_name.to_lowercase().contains("synthia") {
        info!("Detected turn format: synthia");
        synthia()
    } else if model_name.to_lowercase().contains("hermes") {
        info!("Detected turn format: hermes");
        hermes()
    } else if model_name.to_lowercase().contains("airoboros") {
        info!("Detected turn format: airoboros");
        llama2_chat()
    } else if model_name.to_lowercase().contains("neural") {
        info!("Detected turn format: neural");
        neural()
    } else if model_name.to_lowercase().contains("grendel") {
        info!("Detected turn format: grendel");
        grendel()
    } else if model_name.to_lowercase().contains("mistrallite") {
        info!("Detected turn format: mistrallite");
        amazon_mistral_lite()
    } else if model_name.to_lowercase().contains("capybara") {
        info!("Detected turn format: capybara");
        yi_capybara_nous()
    } else if model_name.to_lowercase().contains("deepseek") {
        info!("Detected turn format: deepseek");
        deepseek_coder()
    } else if model_name.to_lowercase().contains("slimorcaboros") {
        info!("Detected turn format: slimorcaboros");
        mistral_slimorcaboros()
    } else if model_name.to_lowercase().contains("orca-2") {
        info!("Detected turn format: orca-2");
        orca2()
    } else if model_name.contains("Xwin-LM") {
        info!("Detected turn format: xwin");
        todo!()
    } else {
        panic!("Unable to detect chat turn format for model id: {model_name}");
    }
}
