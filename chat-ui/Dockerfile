FROM node:20 as base-node-image

RUN git clone --depth 1 https://github.com/huggingface/chat-ui
RUN git checkout 96d25a671f9e6ed6651cbb9cdd62b88bbe5d1bef
WORKDIR /chat-ui

RUN rm package.json && rm .env
COPY ./.env .
COPY ./package.json .

RUN npm install

CMD ["npm", "run", "prod"]