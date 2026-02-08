<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { documentCommentsApi } from '@/api/client'
import { useAuthStore } from '@/stores/auth'

interface Comment {
  id: string
  documentId: string
  parentCommentId?: string
  content: string
  createdBy: string
  createdByName?: string
  createdAt: string
  modifiedAt?: string
  replyCount: number
  replies?: Comment[]
}

const props = defineProps<{
  documentId: string
}>()

const emit = defineEmits<{
  close: []
}>()

const authStore = useAuthStore()
const comments = ref<Comment[]>([])
const isLoading = ref(true)
const newComment = ref('')
const replyingTo = ref<string | null>(null)
const replyContent = ref('')
const editingComment = ref<string | null>(null)
const editContent = ref('')

onMounted(async () => {
  await loadComments()
})

async function loadComments() {
  isLoading.value = true
  try {
    const response = await documentCommentsApi.getByDocument(props.documentId)
    comments.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

async function addComment() {
  if (!newComment.value.trim()) return

  try {
    await documentCommentsApi.create(props.documentId, { content: newComment.value })
    newComment.value = ''
    await loadComments()
  } catch (error) {
  }
}

async function addReply(parentId: string) {
  if (!replyContent.value.trim()) return

  try {
    await documentCommentsApi.create(props.documentId, {
      content: replyContent.value,
      parentCommentId: parentId
    })
    replyingTo.value = null
    replyContent.value = ''
    await loadComments()
  } catch (error) {
  }
}

async function updateComment(commentId: string) {
  if (!editContent.value.trim()) return

  try {
    await documentCommentsApi.update(props.documentId, commentId, { content: editContent.value })
    editingComment.value = null
    editContent.value = ''
    await loadComments()
  } catch (error) {
  }
}

async function deleteComment(commentId: string) {
  if (!confirm('Are you sure you want to delete this comment?')) return

  try {
    await documentCommentsApi.delete(props.documentId, commentId)
    await loadComments()
  } catch (error) {
  }
}

async function loadReplies(commentId: string) {
  try {
    const response = await documentCommentsApi.getReplies(props.documentId, commentId)
    const comment = comments.value.find(c => c.id === commentId)
    if (comment) {
      comment.replies = response.data
    }
  } catch (error) {
  }
}

function startEdit(comment: Comment) {
  editingComment.value = comment.id
  editContent.value = comment.content
}

function cancelEdit() {
  editingComment.value = null
  editContent.value = ''
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString()
}

function isOwner(comment: Comment) {
  return comment.createdBy === authStore.user?.id
}
</script>

<template>
  <div class="flex flex-col h-full bg-white dark:bg-slate-900">
    <!-- Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-800">
      <h2 class="text-lg font-semibold text-slate-900 dark:text-slate-100">Comments</h2>
      <button
        @click="emit('close')"
        class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
      >
        <span class="material-symbols-outlined">close</span>
      </button>
    </div>

    <!-- Comments List -->
    <div class="flex-1 overflow-y-auto p-6 space-y-4">
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <div class="animate-spin w-8 h-8 border-3 border-teal border-t-transparent rounded-full"></div>
      </div>

      <div v-else-if="comments.length === 0" class="text-center py-12">
        <span class="material-symbols-outlined text-4xl text-slate-300 dark:text-slate-600 mb-2">chat</span>
        <p class="text-slate-500 dark:text-slate-400">No comments yet</p>
        <p class="text-sm text-slate-400 dark:text-slate-500">Be the first to comment!</p>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="comment in comments"
          :key="comment.id"
          class="bg-slate-50 dark:bg-slate-800 rounded-xl p-4"
        >
          <!-- Comment Header -->
          <div class="flex items-start justify-between mb-2">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-full bg-teal/20 flex items-center justify-center text-teal text-sm font-semibold">
                {{ comment.createdByName?.charAt(0) || '?' }}
              </div>
              <div>
                <p class="text-sm font-medium text-slate-900 dark:text-slate-100">
                  {{ comment.createdByName || 'Unknown User' }}
                </p>
                <p class="text-xs text-slate-400">{{ formatDate(comment.createdAt) }}</p>
              </div>
            </div>
            <div v-if="isOwner(comment)" class="flex items-center gap-1">
              <button
                @click="startEdit(comment)"
                class="p-1 text-slate-400 hover:text-teal rounded transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-lg">edit</span>
              </button>
              <button
                @click="deleteComment(comment.id)"
                class="p-1 text-slate-400 hover:text-red-500 rounded transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-lg">delete</span>
              </button>
            </div>
          </div>

          <!-- Comment Content or Edit Form -->
          <div v-if="editingComment === comment.id" class="mt-2">
            <textarea
              v-model="editContent"
              class="w-full p-3 border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-sm resize-none focus:ring-2 focus:ring-teal/50 focus:border-teal"
              rows="3"
            ></textarea>
            <div class="flex justify-end gap-2 mt-2">
              <button
                @click="cancelEdit"
                class="px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                @click="updateComment(comment.id)"
                class="px-3 py-1.5 text-sm bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
              >
                Save
              </button>
            </div>
          </div>
          <p v-else class="text-sm text-slate-700 dark:text-slate-300 whitespace-pre-wrap">
            {{ comment.content }}
          </p>

          <!-- Reply Section -->
          <div class="mt-3 pt-3 border-t border-slate-200 dark:border-slate-700">
            <button
              v-if="replyingTo !== comment.id"
              @click="replyingTo = comment.id"
              class="text-sm text-teal hover:text-teal/80 font-medium flex items-center gap-1"
            >
              <span class="material-symbols-outlined text-base">reply</span>
              Reply
              <span v-if="comment.replyCount > 0" class="text-slate-400">({{ comment.replyCount }})</span>
            </button>

            <!-- Reply Form -->
            <div v-if="replyingTo === comment.id" class="mt-3">
              <textarea
                v-model="replyContent"
                placeholder="Write a reply..."
                class="w-full p-3 border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-sm resize-none focus:ring-2 focus:ring-teal/50 focus:border-teal"
                rows="2"
              ></textarea>
              <div class="flex justify-end gap-2 mt-2">
                <button
                  @click="replyingTo = null; replyContent = ''"
                  class="px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="addReply(comment.id)"
                  class="px-3 py-1.5 text-sm bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
                >
                  Reply
                </button>
              </div>
            </div>

            <!-- Show Replies -->
            <div v-if="comment.replyCount > 0 && !comment.replies" class="mt-2">
              <button
                @click="loadReplies(comment.id)"
                class="text-sm text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
              >
                Show {{ comment.replyCount }} {{ comment.replyCount === 1 ? 'reply' : 'replies' }}
              </button>
            </div>

            <!-- Replies List -->
            <div v-if="comment.replies && comment.replies.length > 0" class="mt-3 pl-4 border-l-2 border-slate-200 dark:border-slate-700 space-y-3">
              <div
                v-for="reply in comment.replies"
                :key="reply.id"
                class="bg-white dark:bg-slate-900 rounded-lg p-3"
              >
                <div class="flex items-center gap-2 mb-1">
                  <div class="w-6 h-6 rounded-full bg-slate-200 dark:bg-slate-700 flex items-center justify-center text-slate-600 dark:text-slate-400 text-xs font-semibold">
                    {{ reply.createdByName?.charAt(0) || '?' }}
                  </div>
                  <span class="text-sm font-medium text-slate-700 dark:text-slate-300">{{ reply.createdByName }}</span>
                  <span class="text-xs text-slate-400">{{ formatDate(reply.createdAt) }}</span>
                </div>
                <p class="text-sm text-slate-600 dark:text-slate-400">{{ reply.content }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- New Comment Form -->
    <div class="p-6 border-t border-slate-200 dark:border-slate-800">
      <div class="flex gap-3">
        <div class="w-8 h-8 rounded-full bg-teal flex items-center justify-center text-white text-sm font-semibold flex-shrink-0">
          {{ authStore.user?.firstName?.charAt(0) || authStore.user?.username?.charAt(0) || '?' }}
        </div>
        <div class="flex-1">
          <textarea
            v-model="newComment"
            placeholder="Add a comment..."
            class="w-full p-3 border border-slate-200 dark:border-slate-700 rounded-lg bg-slate-50 dark:bg-slate-800 text-sm resize-none focus:ring-2 focus:ring-teal/50 focus:border-teal focus:bg-white dark:focus:bg-slate-900"
            rows="3"
            @keydown.ctrl.enter="addComment"
          ></textarea>
          <div class="flex justify-between items-center mt-2">
            <span class="text-xs text-slate-400">Ctrl + Enter to submit</span>
            <button
              @click="addComment"
              :disabled="!newComment.trim()"
              class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              Comment
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
