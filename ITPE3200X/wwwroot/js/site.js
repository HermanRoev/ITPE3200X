// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelector('.first-button').addEventListener('click', function () {

    document.querySelector('.animated-icon1').classList.toggle('open');
});

function showFullContent(postId) {
    document.getElementById('postContentShort-' + postId).style.display = 'none';
    document.getElementById('postContentFull-' + postId).style.display = 'block';
}
function showShortContent(postId) {
    document.getElementById('postContentShort-' + postId).style.display = 'block';
    document.getElementById('postContentFull-' + postId).style.display = 'none';
}
function likePost(postId) {
    // Implement the like functionality here
    alert('Liked post ' + postId);
}
function savePost(postId) {
    // Implement the save functionality here
    alert('Saved post ' + postId);
}
