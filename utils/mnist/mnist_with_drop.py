from __future__ import absolute_import
from __future__ import division
from __future__ import print_function

import argparse
import sys
import tensorflow as tf
from tensorflow.examples.tutorials.mnist import input_data
from tensorflow.python.framework import graph_util


FLAGS = None
input_dir = './input_data'
summary_location = './out/summary'
model_save_location = './out/model/'
full_var_model_name = 'model_needcut.pb'
out_var_only_model_name = 'model_minimal.pb'


def weight_variable(shape, name):
    """weight_variable generates a weight variable of a given shape."""
    initial = tf.truncated_normal(shape, stddev=0.1)
    return tf.Variable(initial, name=name)


def bias_variable(shape, name):
    """bias_variable generates a bias variable of a given shape."""
    initial = tf.constant(0.1, shape=shape)
    return tf.Variable(initial, name=name)


def deepnn(x):
    "输入层:"
    x_image = tf.reshape(x, [-1, 28, 28, 1], name="reshape")
    "第一层:卷积+relu"
    w_conv1 = weight_variable([5, 5, 1, 32], name="w1")
    b_conv1 = bias_variable([32], name='b1')
    pre_activation1 = tf.nn.conv2d(x_image, w_conv1, strides=[1, 1, 1, 1], padding='SAME', name='conv1')
    h_conv1 = tf.nn.relu(pre_activation1 + b_conv1, name='activation1')

    "第二层:池化"
    h_pool1 = tf.nn.max_pool(h_conv1, ksize=[1, 2, 2, 1], strides=[1, 2, 2, 1], padding='SAME', name='pool1')
    "第三层:卷积+relu"
    w_conv2 = weight_variable([5, 5, 32, 64], name='w3')
    b_conv2 = bias_variable([64], name='b3')
    pre_activation2 = tf.nn.conv2d(h_pool1, w_conv2, strides=[1, 1, 1, 1], padding='SAME', name="conv2")
    h_conv2 = tf.nn.relu(pre_activation2 + b_conv2, name='activation2')
    "第四层:池化"
    h_pool2 = tf.nn.max_pool(h_conv2, ksize=[1, 2, 2, 1], strides=[1, 2, 2, 1], padding='SAME', name='pool2')
    "第五层:全连接"
    w_fc1 = weight_variable([7 * 7 * 64, 1024], name='w5')
    b_fc1 = bias_variable([1024], name='b5')
    h_pool2_flat = tf.reshape(h_pool2, [-1, 7 * 7 * 64])
    h_fc1 = tf.nn.relu(tf.matmul(h_pool2_flat, w_fc1) + b_fc1, name='fc1')
    "dropout"
    keep_prob = tf.placeholder(tf.float32, name='Keep_prob')
    h_fc1_drop = tf.nn.dropout(h_fc1, keep_prob, name='dropout')
    "第六层:全连接"
    W_fc2 = weight_variable([1024, 10], name='w6')
    b_fc2 = bias_variable([10], name='b6')
    h_fc2 = tf.matmul(h_fc1_drop, W_fc2, name='fc2')
    "第七层:softmax 输出"
    y_conv = tf.nn.softmax(h_fc2 + b_fc2, name='out')


    return y_conv, keep_prob


def main(_):
    # Import data
    mnist = input_data.read_data_sets(FLAGS.data_dir)

    # Create the model
    x = tf.placeholder(tf.float32, [None, 784], name='input')

    # Define loss and optimizer
    y_ = tf.placeholder(tf.int64, [None],name="y_")

    # Build the graph for the deep net
    y_conv, keep_prob = deepnn(x)

    cross_entropy = tf.losses.sparse_softmax_cross_entropy(labels=y_, logits=y_conv)
    cross_entropy = tf.reduce_mean(cross_entropy, name='loss')

    train_step = tf.train.AdamOptimizer(1e-4).minimize(cross_entropy, name='adam_optimizer')

    correct_prediction = tf.equal(tf.argmax(y_conv, 1), y_)
    correct_prediction = tf.cast(correct_prediction, tf.float32)
    accuracy = tf.reduce_mean(correct_prediction, name='accuracy')

    print('Saving graph to: %s' % summary_location)
    train_writer = tf.summary.FileWriter(summary_location)
    train_writer.add_graph(tf.get_default_graph())

    gpu_options = tf.GPUOptions(per_process_gpu_memory_fraction=0.5)
    with tf.Session(config=tf.ConfigProto(gpu_options=gpu_options)) as sess:
        sess.run(tf.global_variables_initializer())

        for i in range(20000):
            batch = mnist.train.next_batch(50)
            if i % 100 == 0:
                train_accuracy = accuracy.eval(feed_dict={
                    x: batch[0], y_: batch[1], keep_prob: 1.0})
                print('step %d, training accuracy %g' % (i, train_accuracy))
            train_step.run(feed_dict={x: batch[0], y_: batch[1], keep_prob: 0.5})

        print('test accuracy %g' % accuracy.eval(feed_dict={
            x: mnist.test.images, y_: mnist.test.labels, keep_prob: 1.0}))

        print('Save Model in File...')

        # 保存方法1
        var_list = ["input","w1","b1","conv1","activation1","pool1","w3","b3",
                    "conv2","activation2","pool2","w5","b5","fc1","Keep_prob",
                   "w6","b6","fc2","out"]
        constant_graph = graph_util.convert_variables_to_constants(sess, sess.graph_def,
                                                                   output_node_names=
                                                                   [var_list[i] for i in range(len(var_list))])
        tf.train.write_graph(constant_graph, model_save_location, full_var_model_name, as_text=False)
        # 保存方法2
        constant_graph = graph_util.convert_variables_to_constants(sess, sess.graph_def, ["out"])

        tf.train.write_graph(constant_graph, model_save_location, out_var_only_model_name, as_text=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--data_dir', type=str, default=input_dir, help='Directory for storing input data')
    FLAGS, unparsed = parser.parse_known_args()
    tf.app.run(main=main, argv=[sys.argv[0]] + unparsed)
