{{/* Default deployment name */}}
{{- define "fullName" -}}
  {{- default .Chart.Name .Values.override.name -}}
{{- end -}}

{{/* Default namespace */}}
{{- define "namespace" -}}
  {{- default (include "fullName" .) .Values.override.namespace -}}
{{- end -}}

{{/* Check replicaCount is less than maxReplicaCount */}}
{{- define "maxCount" -}}
  {{- if lt (int .Values.scale.maxReplicaCount) 1 -}}
    {{- fail "scale.maxReplicaCount cannot be less than 1" -}}
  {{- end -}}
  {{- if gt (int .Values.scale.replicaCount) (int .Values.scale.maxReplicaCount) -}}
    {{- fail "scale.replicaCount cannot be greater than scale.maxReplicaCount" -}}
  {{- else -}}
    {{- printf "%d" (int .Values.scale.maxReplicaCount) -}}
  {{- end -}}
{{- end -}}

{{/* Check if host is set */}}
{{- define "hostName" -}}
  {{- if .Values.host -}}
    {{- printf "%s" .Values.host -}}
  {{- else -}}
    {{- fail "You need to specify a host" -}}
  {{- end -}}
{{- end -}}

{{/* Check if image.domain is set */}}
{{- define "imageDomain" -}}
  {{- if .Values.image.domain -}}
    {{- printf "%s" .Values.image.domain -}}
  {{- else -}}
    {{- fail "You need to specify image.domain" -}}
  {{- end -}}
{{- end -}}

{{/* Check if public.ip is set */}}
{{- define "publicIP" -}}
  {{- if .Values.public.ip -}}
    {{- printf "%s" .Values.public.ip -}}
  {{- else -}}
    {{- fail "You need to specify public.ip" -}}
  {{- end -}}
{{- end -}}
