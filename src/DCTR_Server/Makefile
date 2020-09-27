APP?=dctrserver
PORT?=8001

RELEASE?=0.0.1

GOOS?=linux
GOARCH?=amd64

clean:
	rm -f ${APP}

build: clean
	CGO_ENABLE=0 GOOS=${GOOS} GOARCH=${GOARCH} go build -o ${APP}

container: build
	docker build -t $(APP):$(RELEASE) .

run: container
	docker stop $(APP):$(RELEASE) || true && docker rm $(APP):$(RELEASE) || true
	docker run --name ${APP} -p ${PORT}:${PORT} --rm \
			-e "PORT=${PORT}" \
			$(APP):$(RELEASE)
