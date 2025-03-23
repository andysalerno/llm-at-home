'use client'

import {
    Text,
    Input,
    Combobox,
    Option,
    Label,
} from '@fluentui/react-components';
import { useConfig } from '@/app/hooks/useConfig';
import { INSTRUCTION_STRATEGIES } from '@/app/types';

const ConfigSection: React.FC = () => {
    const { config, updateConfig } = useConfig();

    return (
        <div className='h-full p-6'>
            <Text size={600} weight="semibold">Configuration</Text>
            <div className="p-6 space-y-6">
                <div>
                    <Label className="block mb-2">
                        Prompt strategy: tools list
                    </Label>
                    <Combobox
                        placeholder='Select a prompt strategy plz'
                        defaultValue={config.instructionStrategy}
                        value={config.instructionStrategy}
                        onOptionSelect={(_, option) => updateConfig({ instructionStrategy: option.optionText })}
                    >
                        {INSTRUCTION_STRATEGIES.map((option) => (
                            <Option key={option}>
                                {option}
                            </Option>
                        ))}
                    </Combobox>
                </div>

                <div>
                    <Label className="block mb-2">
                        Chat completions endpoint
                    </Label>
                    <Input
                        value={config.apiEndpoint}
                        onChange={(e) => updateConfig({ apiEndpoint: e.target.value })}
                        placeholder="Enter api endpoint"
                        className="w-full"
                    />
                </div>

                <div>
                    <Label className="block mb-2">
                        Bearer token (optional)
                    </Label>
                    <Input
                        value={config.bearerToken}
                        onChange={(e) => updateConfig({ bearerToken: e.target.value })}
                        placeholder="Enter bearer token"
                        className="w-full"
                    />
                </div>

                <div>
                    <Label className="block mb-2">
                        Model name
                    </Label>
                    <Input
                        value={config.modelName}
                        onChange={(e) => updateConfig({ modelName: e.target.value })}
                        placeholder="Enter model name"
                        className="w-full"
                    />
                </div>
            </div>
        </div >
    );
};

export default ConfigSection;