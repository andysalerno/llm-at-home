FROM node:20 as base-node-image

# copy the content to the target container
COPY . app
WORKDIR /app

RUN npm install

CMD ["npm", "run", "prod"]