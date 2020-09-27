APP?=dctrserver
PORT?=8001

RELEASE?=0.0.1
CONTAINER_IMAGE?=docker.io/ryutensai/${APP}

GOOS?=linux
GOARCH?=amd64

clean:
	rm -f ${APP}

build: clean
	CGO_ENABLE=0 GOOS=${GOOS} GOARCH=${GOARCH} go build -o ${APP}

container: build
	docker build -t $(CONTAINER_IMAGE):$(RELEASE) .

run: container
	docker stop $(APP):$(RELEASE) || true && docker rm $(APP):$(RELEASE) || true
	docker run --name ${APP} -p ${PORT}:${PORT} --rm \
			-e "PORT=${PORT}" \
			$(APP):$(RELEASE)

push: container
	docker push $(CONTAINER_IMAGE):$(RELEASE)
